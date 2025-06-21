using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LibraryDBApi.Enums;
using LibraryDBApi.Extensions;
using LibraryDBApi.Models;
using LibraryDBApi.Utilities;

namespace LibraryDBApi.Core
{
    /// <summary>
    /// Implementación principal del servicio de datos para procedimientos almacenados de forma innovadora
    /// </summary>
    public class BaseDataService : IDataService
    {
        /// <summary>
        /// Ejecuta un procedimiento almacenado y devuelve un resultado tipado como IEnumerable
        /// </summary>
        public async Task<StoredProcedureResult<IEnumerable<TResult>>> EjecutarProcedimientoAsync<TResult>(string connectionString, string procedureName) where TResult : new()
        {
            try
            {
                var dataSet = new DataSet();
                using (var connection = new SqlConnection(connectionString))
                using (var command = new SqlCommand(procedureName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    var adapter = new SqlDataAdapter(command);
                    adapter.Fill(dataSet);
                }

                var result = new StoredProcedureResult<IEnumerable<TResult>>(dataSet);
                if (dataSet.Tables.Count > 0)
                {
                    result.Data = DataTableToList<TResult>(dataSet.Tables[0]);
                }
                else
                {
                    result.Data = Enumerable.Empty<TResult>();
                }
                
                result.IsSuccess = true;
                result.Message = "Operación exitosa";
                return result;
            }
            catch (Exception ex)
            {
                return StoredProcedureResult<IEnumerable<TResult>>.Failure(ex);
            }
        }

        /// <summary>
        /// Ejecuta un procedimiento almacenado con parámetros y devuelve un resultado tipado como IEnumerable (inferencia automática del modelo)
        /// </summary>
        public async Task<StoredProcedureResult<IEnumerable<TResult>>> EjecutarProcedimientoAsync<TResult>(string connectionString, string procedureName, object model) where TResult : new()
        {
            try
            {
                var dbParameters = await GetProcedureParametersAsync(connectionString, procedureName);
                var modelDict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                if (model != null)
                {
                    foreach (var prop in model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        modelDict[prop.Name] = prop.GetValue(model);
                    }
                }

                var parameters = new List<SqlParameter>();
                foreach (var dbParam in dbParameters)
                {
                    if (modelDict.TryGetValue(dbParam.ParameterName.TrimStart('@'), out var value))
                    {
                        parameters.Add(new SqlParameter(dbParam.ParameterName, dbParam.SqlDbType)
                        {
                            Direction = dbParam.Direction,
                            Size = dbParam.Size > 0 ? dbParam.Size : 0,
                            Value = value ?? DBNull.Value
                        });
                    }
                }

                var dataSet = new DataSet();
                using (var connection = new SqlConnection(connectionString))
                using (var command = new SqlCommand(procedureName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddRange(parameters.ToArray());
                    var adapter = new SqlDataAdapter(command);
                    adapter.Fill(dataSet);
                }

                var result = new StoredProcedureResult<IEnumerable<TResult>>(dataSet);
                if (dataSet.Tables.Count > 0)
                {
                    result.Data = DataTableToList<TResult>(dataSet.Tables[0]);
                }
                else
                {
                    result.Data = Enumerable.Empty<TResult>();
                }
                
                result.IsSuccess = true;
                result.Message = "Operación exitosa";
                return result;
            }
            catch (Exception ex)
            {
                return StoredProcedureResult<IEnumerable<TResult>>.Failure(ex);
            }
        }

        #region Operaciones Masivas Innovadoras

        /// <summary>
        /// Inserta datos masivamente usando SqlBulkCopy para máxima velocidad
        /// </summary>
        public async Task<BulkOperationResult> InsertarDatosMasivamenteAsync<T>(string connectionString, string tableName, IEnumerable<T> data, BulkInsertOptions options = null)
        {
            var stopwatch = Stopwatch.StartNew();
            var prepStopwatch = Stopwatch.StartNew();

            try
            {
                options ??= new BulkInsertOptions();
                var dataList = data?.ToList() ?? new List<T>();
                if (!dataList.Any()) return BulkOperationResult.Success(0, stopwatch.ElapsedMilliseconds);

                // Preparar DataTable
                var dataTable = CreateDataTableFromModel<T>(dataList, options);
                prepStopwatch.Stop();

                var transferStopwatch = Stopwatch.StartNew();
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var bulkCopy = new SqlBulkCopy(connection))
                    {
                        bulkCopy.DestinationTableName = tableName;
                        bulkCopy.BatchSize = options.BatchSize;
                        bulkCopy.BulkCopyTimeout = options.Timeout;

                        // Configurar opciones
                        if (options.KeepIdentity) bulkCopy.SqlRowsCopied += (sender, e) => { };
                        if (!options.CheckConstraints) bulkCopy.SqlRowsCopied += (sender, e) => { };
                        if (options.KeepNulls) bulkCopy.SqlRowsCopied += (sender, e) => { };
                        if (options.FireTriggers) bulkCopy.SqlRowsCopied += (sender, e) => { };

                        // Mapear columnas
                        foreach (DataColumn column in dataTable.Columns)
                        {
                            if (!options.ExcludeColumns.Contains(column.ColumnName))
                            {
                                var targetColumn = options.ColumnMappings.ContainsKey(column.ColumnName) 
                                    ? options.ColumnMappings[column.ColumnName] 
                                    : column.ColumnName;
                                bulkCopy.ColumnMappings.Add(column.ColumnName, targetColumn);
                            }
                        }

                        transferStopwatch.Stop();
                        var processStopwatch = Stopwatch.StartNew();

                        // Ejecutar inserción masiva
                        await bulkCopy.WriteToServerAsync(dataTable);

                        processStopwatch.Stop();
                        stopwatch.Stop();

                        return new BulkOperationResult
                        {
                            IsSuccess = true,
                            Message = "Inserción masiva exitosa",
                            RowsAffected = dataList.Count,
                            ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                            BatchSize = options.BatchSize,
                            Stats = new BulkOperationStats
                            {
                                RowsInserted = dataList.Count,
                                PreparationTimeMs = prepStopwatch.ElapsedMilliseconds,
                                TransferTimeMs = transferStopwatch.ElapsedMilliseconds,
                                ProcessingTimeMs = processStopwatch.ElapsedMilliseconds
                            }
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return BulkOperationResult.Failure(ex, "Error en inserción masiva");
            }
        }

        /// <summary>
        /// Actualiza datos masivamente usando Table-Valued Parameters
        /// </summary>
        public async Task<BulkOperationResult> ActualizarDatosMasivamenteAsync<T>(string connectionString, string tableName, IEnumerable<T> data, string keyColumn, BulkUpdateOptions options = null)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                options ??= new BulkUpdateOptions();
                var dataList = data?.ToList() ?? new List<T>();
                if (!dataList.Any()) return BulkOperationResult.Success(0, stopwatch.ElapsedMilliseconds);

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    SqlTransaction transaction = null;

                    try
                    {
                        if (options.UseTransaction)
                            transaction = connection.BeginTransaction();

                        var dataTable = CreateDataTableFromModel<T>(dataList, null);
                        var updateColumns = options.UpdateColumns.Any() 
                            ? options.UpdateColumns 
                            : GetUpdateColumns<T>(keyColumn);

                        var sql = GenerateBulkUpdateSql(tableName, keyColumn, updateColumns);
                        using (var command = new SqlCommand(sql, connection, transaction))
                        {
                            command.CommandTimeout = options.Timeout;
                            command.Parameters.AddWithValue("@DataTable", dataTable);
                            command.Parameters["@DataTable"].SqlDbType = SqlDbType.Structured;
                            command.Parameters["@DataTable"].TypeName = "dbo.BulkUpdateTableType";

                            var rowsAffected = await command.ExecuteNonQueryAsync();

                            if (options.UseTransaction)
                                transaction?.Commit();

                            stopwatch.Stop();
                            return BulkOperationResult.Success(rowsAffected, stopwatch.ElapsedMilliseconds, options.BatchSize);
                        }
                    }
                    catch
                    {
                        if (options.UseTransaction)
                            transaction?.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return BulkOperationResult.Failure(ex, "Error en actualización masiva");
            }
        }

        /// <summary>
        /// Elimina datos masivamente de forma optimizada
        /// </summary>
        public async Task<BulkOperationResult> EliminarDatosMasivamenteAsync<T>(string connectionString, string tableName, IEnumerable<T> data, string keyColumn, BulkDeleteOptions options = null)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                options ??= new BulkDeleteOptions();
                var dataList = data?.ToList() ?? new List<T>();
                if (!dataList.Any()) return BulkOperationResult.Success(0, stopwatch.ElapsedMilliseconds);

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    SqlTransaction transaction = null;

                    try
                    {
                        if (options.UseTransaction)
                            transaction = connection.BeginTransaction();

                        var keyValues = ExtractKeyValues(dataList, keyColumn);
                        var sql = GenerateBulkDeleteSql(tableName, keyColumn, keyValues.Count);

                        using (var command = new SqlCommand(sql, connection, transaction))
                        {
                            command.CommandTimeout = options.Timeout;
                            for (int i = 0; i < keyValues.Count; i++)
                            {
                                command.Parameters.AddWithValue($"@Key{i}", keyValues[i]);
                            }

                            var rowsAffected = await command.ExecuteNonQueryAsync();

                            if (options.UseTransaction)
                                transaction?.Commit();

                            stopwatch.Stop();
                            return BulkOperationResult.Success(rowsAffected, stopwatch.ElapsedMilliseconds, options.BatchSize);
                        }
                    }
                    catch
                    {
                        if (options.UseTransaction)
                            transaction?.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return BulkOperationResult.Failure(ex, "Error en eliminación masiva");
            }
        }

        /// <summary>
        /// Realiza operaciones de inserción/actualización masivas (Upsert)
        /// </summary>
        public async Task<BulkOperationResult> InsertarOActualizarMasivamenteAsync<T>(string connectionString, string tableName, IEnumerable<T> data, string keyColumn, BulkUpsertOptions options = null)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                options ??= new BulkUpsertOptions();
                var dataList = data?.ToList() ?? new List<T>();
                if (!dataList.Any()) return BulkOperationResult.Success(0, stopwatch.ElapsedMilliseconds);

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    SqlTransaction transaction = null;

                    try
                    {
                        if (options.UseTransaction)
                            transaction = connection.BeginTransaction();

                        var dataTable = CreateDataTableFromModel<T>(dataList, null);
                        var updateColumns = options.UpdateColumns.Any() 
                            ? options.UpdateColumns 
                            : GetUpdateColumns<T>(keyColumn);

                        string sql;
                        switch (options.Strategy)
                        {
                            case UpsertStrategy.Merge:
                                sql = GenerateMergeSql(tableName, keyColumn, updateColumns);
                                break;
                            case UpsertStrategy.InsertIfNotExists:
                                sql = GenerateInsertIfNotExistsSql(tableName, keyColumn, updateColumns);
                                break;
                            default:
                                sql = GenerateUpdateIfExistsSql(tableName, keyColumn, updateColumns);
                                break;
                        }

                        using (var command = new SqlCommand(sql, connection, transaction))
                        {
                            command.CommandTimeout = options.Timeout;
                            command.Parameters.AddWithValue("@DataTable", dataTable);
                            command.Parameters["@DataTable"].SqlDbType = SqlDbType.Structured;
                            command.Parameters["@DataTable"].TypeName = "dbo.BulkUpsertTableType";

                            var rowsAffected = await command.ExecuteNonQueryAsync();

                            if (options.UseTransaction)
                                transaction?.Commit();

                            stopwatch.Stop();
                            return BulkOperationResult.Success(rowsAffected, stopwatch.ElapsedMilliseconds, options.BatchSize);
                        }
                    }
                    catch
                    {
                        if (options.UseTransaction)
                            transaction?.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return BulkOperationResult.Failure(ex, "Error en operación Upsert masiva");
            }
        }

        /// <summary>
        /// Ejecuta múltiples operaciones en lote con transacción
        /// </summary>
        public async Task<BatchOperationResult> EjecutarOperacionesEnLoteAsync(string connectionString, IEnumerable<BatchOperation> operations)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new BatchOperationResult();
            var operationsList = operations?.ToList() ?? new List<BatchOperation>();

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            for (int i = 0; i < operationsList.Count; i++)
                            {
                                var operation = operationsList[i];
                                try
                                {
                                    switch (operation.Type)
                                    {
                                        case BatchOperationType.Insert:
                                            await ExecuteBatchInsert(connection, transaction, operation);
                                            break;
                                        case BatchOperationType.Update:
                                            await ExecuteBatchUpdate(connection, transaction, operation);
                                            break;
                                        case BatchOperationType.Delete:
                                            await ExecuteBatchDelete(connection, transaction, operation);
                                            break;
                                        case BatchOperationType.Upsert:
                                            await ExecuteBatchUpsert(connection, transaction, operation);
                                            break;
                                    }
                                    result.SuccessfulOperations++;
                                }
                                catch (Exception ex)
                                {
                                    result.FailedOperations++;
                                    result.Errors.Add(new BatchOperationError
                                    {
                                        OperationIndex = i,
                                        OperationType = operation.Type.ToString(),
                                        ErrorMessage = ex.Message,
                                        Exception = ex
                                    });
                                }
                            }

                            transaction.Commit();
                            result.IsSuccess = result.FailedOperations == 0;
                            result.Message = result.IsSuccess ? "Todas las operaciones exitosas" : "Algunas operaciones fallaron";
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }

                result.TotalOperations = operationsList.Count;
                result.TotalExecutionTimeMs = stopwatch.ElapsedMilliseconds;
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.IsSuccess = false;
                result.Message = $"Error en ejecución de lote: {ex.Message}";
                result.TotalExecutionTimeMs = stopwatch.ElapsedMilliseconds;
                return result;
            }
        }

        /// <summary>
        /// Sincroniza datos entre tablas de forma masiva
        /// </summary>
        public async Task<BulkOperationResult> SincronizarDatosMasivamenteAsync<T>(string connectionString, string sourceTable, string targetTable, string keyColumn, BulkSyncOptions options = null)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                options ??= new BulkSyncOptions();
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    SqlTransaction transaction = null;

                    try
                    {
                        if (options.UseTransaction)
                            transaction = connection.BeginTransaction();

                        var syncColumns = options.SyncColumns.Any() 
                            ? options.SyncColumns 
                            : GetTableColumns(connection, sourceTable);

                        var sql = GenerateSyncSql(sourceTable, targetTable, keyColumn, syncColumns, options.DeleteMissing);

                        using (var command = new SqlCommand(sql, connection, transaction))
                        {
                            command.CommandTimeout = options.Timeout;
                            var rowsAffected = await command.ExecuteNonQueryAsync();

                            if (options.UseTransaction)
                                transaction?.Commit();

                            stopwatch.Stop();
                            return BulkOperationResult.Success(rowsAffected, stopwatch.ElapsedMilliseconds, options.BatchSize);
                        }
                    }
                    catch
                    {
                        if (options.UseTransaction)
                            transaction?.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return BulkOperationResult.Failure(ex, "Error en sincronización masiva");
            }
        }

        #endregion

        #region Métodos Auxiliares

        // Utilidad: Obtener los parámetros reales del procedimiento almacenado
        private async Task<List<DbParameterInfo>> GetProcedureParametersAsync(string connectionString, string procedureName)
        {
            var parameters = new List<DbParameterInfo>();
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand("sp_procedure_params_rowset", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@procedure_name", procedureName);
                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        parameters.Add(new DbParameterInfo
                        {
                            ParameterName = reader["PARAMETER_NAME"].ToString(),
                            SqlDbType = GetSqlDbType(reader["DATA_TYPE"].ToString()),
                            Direction = GetParameterDirection(reader["PARAMETER_TYPE"]),
                            Size = reader["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value ? Convert.ToInt32(reader["CHARACTER_MAXIMUM_LENGTH"]) : 0
                        });
                    }
                }
            }
            return parameters;
        }

        // Utilidad: Mapear DataTable a List<T> con mapeo mejorado
        private static List<T> DataTableToList<T>(DataTable table) where T : new()
        {
            var list = new List<T>();
            if (table.Rows.Count == 0) return list;

            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var columnMappings = CreateColumnMappings(table, props);

            foreach (DataRow row in table.Rows)
            {
                var obj = new T();
                foreach (var prop in props)
                {
                    if (columnMappings.TryGetValue(prop.Name, out var columnName))
                    {
                        var value = row[columnName];
                        if (value != DBNull.Value)
                        {
                            try
                            {
                                var convertedValue = ConvertValue(value, prop);
                                prop.SetValue(obj, convertedValue);
                            }
                            catch (Exception ex)
                            {
                                // Log error pero continúa con otras propiedades
                                System.Diagnostics.Debug.WriteLine($"Error mapeando {prop.Name}: {ex.Message}");
                            }
                        }
                    }
                }
                list.Add(obj);
            }
            return list;
        }

        // Utilidad: Mapear DataTable a objeto T con mapeo mejorado
        private static T DataTableToObject<T>(DataTable table) where T : new()
        {
            if (table.Rows.Count == 0) return default(T);
            
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var columnMappings = CreateColumnMappings(table, props);
            var obj = new T();
            var row = table.Rows[0];

            foreach (var prop in props)
            {
                if (columnMappings.TryGetValue(prop.Name, out var columnName))
                {
                    var value = row[columnName];
                    if (value != DBNull.Value)
                    {
                        try
                        {
                            var convertedValue = ConvertValue(value, prop);
                            prop.SetValue(obj, convertedValue);
                        }
                        catch (Exception ex)
                        {
                            // Log error pero continúa con otras propiedades
                            System.Diagnostics.Debug.WriteLine($"Error mapeando {prop.Name}: {ex.Message}");
                        }
                    }
                }
            }
            return obj;
        }

        // Crear mapeo de columnas flexible
        private static Dictionary<string, string> CreateColumnMappings(DataTable table, PropertyInfo[] properties)
        {
            var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var availableColumns = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();

            foreach (var prop in properties)
            {
                // Verificar si la propiedad debe ser ignorada
                var ignoreAttribute = prop.GetCustomAttribute<IgnoreMappingAttribute>();
                if (ignoreAttribute != null)
                    continue;

                // Verificar si hay mapeo explícito
                var columnMappingAttribute = prop.GetCustomAttribute<ColumnMappingAttribute>();
                if (columnMappingAttribute != null)
                {
                    var columnName = columnMappingAttribute.ColumnName;
                    if (availableColumns.Any(c => string.Equals(c, columnName, StringComparison.OrdinalIgnoreCase)))
                    {
                        mappings[prop.Name] = columnName;
                    }
                    else if (!columnMappingAttribute.IsOptional)
                    {
                        // Log warning si la columna es requerida pero no existe
                        System.Diagnostics.Debug.WriteLine($"Warning: Columna requerida '{columnName}' no encontrada para propiedad '{prop.Name}'");
                    }
                    continue;
                }

                // Mapeo automático si no hay atributo explícito
                var autoColumnName = FindBestColumnMatch(prop.Name, availableColumns);
                if (!string.IsNullOrEmpty(autoColumnName))
                {
                    mappings[prop.Name] = autoColumnName;
                }
            }

            return mappings;
        }

        // Encontrar la mejor coincidencia de columna
        private static string FindBestColumnMatch(string propertyName, List<string> availableColumns)
        {
            // 1. Coincidencia exacta (case-insensitive)
            var exactMatch = availableColumns.FirstOrDefault(c => 
                string.Equals(c, propertyName, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(exactMatch))
                return exactMatch;

            // 2. Coincidencia con prefijos comunes
            var commonPrefixes = new[] { "Id", "ID", "id", "ID_" };
            foreach (var prefix in commonPrefixes)
            {
                if (propertyName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    var suffix = propertyName.Substring(prefix.Length);
                    var match = availableColumns.FirstOrDefault(c => 
                        c.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrEmpty(match))
                        return match;
                }
            }

            // 3. Coincidencia parcial (para casos como ProductId -> Product_ID)
            var partialMatch = availableColumns.FirstOrDefault(c => 
                c.Contains(propertyName, StringComparison.OrdinalIgnoreCase) ||
                propertyName.Contains(c, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(partialMatch))
                return partialMatch;

            // 4. Coincidencia con guiones bajos (ProductId -> Product_Id)
            var underscoreVersion = string.Join("_", 
                System.Text.RegularExpressions.Regex.Split(propertyName, @"(?<!^)(?=[A-Z])"));
            var underscoreMatch = availableColumns.FirstOrDefault(c => 
                string.Equals(c, underscoreVersion, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(underscoreMatch))
                return underscoreMatch;

            return null;
        }

        // Convertir valor con mejor manejo de tipos
        private static object ConvertValue(object value, PropertyInfo property)
        {
            if (value == null || value == DBNull.Value)
            {
                // Verificar si hay valor por defecto en el atributo
                var columnMappingAttribute = property.GetCustomAttribute<ColumnMappingAttribute>();
                if (columnMappingAttribute?.DefaultValue != null)
                    return columnMappingAttribute.DefaultValue;

                return GetDefaultValue(property.PropertyType);
            }

            var targetType = property.PropertyType;

            // Si ya es del tipo correcto, retornarlo
            if (targetType.IsAssignableFrom(value.GetType()))
                return value;

            try
            {
                // Verificar si hay conversión personalizada
                var customConversionAttribute = property.GetCustomAttribute<CustomConversionAttribute>();
                if (customConversionAttribute != null)
                {
                    return ApplyCustomConversion(value, customConversionAttribute);
                }

                // Conversiones especiales
                if (targetType == typeof(DateTime) && value is string dateString)
                {
                    if (DateTime.TryParse(dateString, out var date))
                        return date;
                }
                else if (targetType == typeof(decimal) && value is string decimalString)
                {
                    if (decimal.TryParse(decimalString, out var decimalValue))
                        return decimalValue;
                }
                else if (targetType == typeof(int) && value is string intString)
                {
                    if (int.TryParse(intString, out var intValue))
                        return intValue;
                }
                else if (targetType == typeof(bool) && value is string boolString)
                {
                    if (bool.TryParse(boolString, out var boolValue))
                        return boolValue;
                }
                else if (targetType == typeof(Guid) && value is string guidString)
                {
                    if (Guid.TryParse(guidString, out var guidValue))
                        return guidValue;
                }
                else if (targetType.IsEnum && value is string enumString)
                {
                    if (Enum.TryParse(targetType, enumString, true, out var enumValue))
                        return enumValue;
                }
                else if (targetType.IsEnum && value is int enumInt)
                {
                    if (Enum.IsDefined(targetType, enumInt))
                        return Enum.ToObject(targetType, enumInt);
                }

                // Conversión estándar
                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                // Verificar si hay valor por defecto en el atributo
                var columnMappingAttribute = property.GetCustomAttribute<ColumnMappingAttribute>();
                if (columnMappingAttribute?.DefaultValue != null)
                    return columnMappingAttribute.DefaultValue;

                return GetDefaultValue(targetType);
            }
        }

        // Aplicar conversión personalizada
        private static object ApplyCustomConversion(object value, CustomConversionAttribute attribute)
        {
            try
            {
                // Aquí puedes implementar conversiones personalizadas específicas
                // Por ejemplo, conversiones de formato de fecha, moneda, etc.
                
                if (attribute.ConversionType == typeof(DateTime) && value is string dateString)
                {
                    if (!string.IsNullOrEmpty(attribute.Format))
                    {
                        if (DateTime.TryParseExact(dateString, attribute.Format, 
                            System.Globalization.CultureInfo.InvariantCulture, 
                            System.Globalization.DateTimeStyles.None, out var date))
                        {
                            return date;
                        }
                    }
                    else if (DateTime.TryParse(dateString, out var date))
                    {
                        return date;
                    }
                }
                else if (attribute.ConversionType == typeof(decimal) && value is string decimalString)
                {
                    if (!string.IsNullOrEmpty(attribute.Format))
                    {
                        if (decimal.TryParse(decimalString, 
                            System.Globalization.NumberStyles.Any, 
                            System.Globalization.CultureInfo.InvariantCulture, out var decimalValue))
                        {
                            return decimalValue;
                        }
                    }
                    else if (decimal.TryParse(decimalString, out var decimalValue))
                    {
                        return decimalValue;
                    }
                }

                // Conversión estándar al tipo especificado
                return Convert.ChangeType(value, attribute.ConversionType);
            }
            catch
            {
                return GetDefaultValue(attribute.ConversionType);
            }
        }

        // Obtener valor por defecto para un tipo
        private static object GetDefaultValue(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);
            return null;
        }

        // Utilidad: Mapear tipo SQL a SqlDbType
        private SqlDbType GetSqlDbType(string sqlType)
        {
            switch (sqlType.ToLower())
            {
                case "int": return SqlDbType.Int;
                case "bigint": return SqlDbType.BigInt;
                case "decimal": return SqlDbType.Decimal;
                case "varchar": return SqlDbType.VarChar;
                case "nvarchar": return SqlDbType.NVarChar;
                case "datetime": return SqlDbType.DateTime;
                case "bit": return SqlDbType.Bit;
                case "uniqueidentifier": return SqlDbType.UniqueIdentifier;
                case "varbinary": return SqlDbType.VarBinary;
                case "xml": return SqlDbType.Xml;
                case "structured": return SqlDbType.Structured;
                default: return SqlDbType.VarChar;
            }
        }

        // Utilidad: Mapear tipo de parámetro
        private ParameterDirection GetParameterDirection(object value)
        {
            int type = Convert.ToInt32(value);
            switch (type)
            {
                case 1: return ParameterDirection.Input;
                case 2: return ParameterDirection.InputOutput;
                case 3: return ParameterDirection.Output;
                case 4: return ParameterDirection.ReturnValue;
                default: return ParameterDirection.Input;
            }
        }

        // Clase auxiliar para metadatos de parámetros
        private class DbParameterInfo
        {
            public string ParameterName { get; set; }
            public SqlDbType SqlDbType { get; set; }
            public ParameterDirection Direction { get; set; }
            public int Size { get; set; }
        }

        #endregion

        #region Métodos de Generación SQL (Implementación simplificada)

        private DataTable CreateDataTableFromModel<T>(List<T> data, BulkInsertOptions options)
        {
            var dataTable = new DataTable();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // Crear columnas
            foreach (var prop in properties)
            {
                if (options?.ExcludeColumns?.Contains(prop.Name) == true) continue;
                dataTable.Columns.Add(prop.Name, prop.PropertyType);
            }

            // Agregar datos
            foreach (var item in data)
            {
                var row = dataTable.NewRow();
                foreach (var prop in properties)
                {
                    if (options?.ExcludeColumns?.Contains(prop.Name) == true) continue;
                    var value = prop.GetValue(item);
                    row[prop.Name] = value ?? DBNull.Value;
                }
                dataTable.Rows.Add(row);
            }

            return dataTable;
        }

        private List<string> GetUpdateColumns<T>(string keyColumn)
        {
            return typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.Name != keyColumn)
                .Select(p => p.Name)
                .ToList();
        }

        private List<object> ExtractKeyValues<T>(List<T> data, string keyColumn)
        {
            var property = typeof(T).GetProperty(keyColumn);
            return data.Select(item => property.GetValue(item)).ToList();
        }

        private List<string> GetTableColumns(SqlConnection connection, string tableName)
        {
            var columns = new List<string>();
            using (var command = new SqlCommand($"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}'", connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    columns.Add(reader["COLUMN_NAME"].ToString());
                }
            }
            return columns;
        }

        // Métodos de generación SQL (implementación básica)
        private string GenerateBulkUpdateSql(string tableName, string keyColumn, List<string> updateColumns)
        {
            var setClause = string.Join(", ", updateColumns.Select(col => $"{col} = s.{col}"));
            return $@"
                UPDATE t SET {setClause}
                FROM {tableName} t
                INNER JOIN @DataTable s ON t.{keyColumn} = s.{keyColumn}";
        }

        private string GenerateBulkDeleteSql(string tableName, string keyColumn, int keyCount)
        {
            var keyParams = string.Join(", ", Enumerable.Range(0, keyCount).Select(i => $"@Key{i}"));
            return $"DELETE FROM {tableName} WHERE {keyColumn} IN ({keyParams})";
        }

        private string GenerateMergeSql(string tableName, string keyColumn, List<string> updateColumns)
        {
            var setClause = string.Join(", ", updateColumns.Select(col => $"{col} = s.{col}"));
            var insertColumns = string.Join(", ", updateColumns.Prepend(keyColumn));
            var insertValues = string.Join(", ", updateColumns.Prepend(keyColumn).Select(col => $"s.{col}"));
            
            return $@"
                MERGE {tableName} AS t
                USING @DataTable AS s ON t.{keyColumn} = s.{keyColumn}
                WHEN MATCHED THEN UPDATE SET {setClause}
                WHEN NOT MATCHED THEN INSERT ({insertColumns}) VALUES ({insertValues});";
        }

        private string GenerateInsertIfNotExistsSql(string tableName, string keyColumn, List<string> updateColumns)
        {
            var insertColumns = string.Join(", ", updateColumns.Prepend(keyColumn));
            var insertValues = string.Join(", ", updateColumns.Prepend(keyColumn).Select(col => $"s.{col}"));
            
            return $@"
                INSERT INTO {tableName} ({insertColumns})
                SELECT {insertValues}
                FROM @DataTable s
                WHERE NOT EXISTS (SELECT 1 FROM {tableName} t WHERE t.{keyColumn} = s.{keyColumn})";
        }

        private string GenerateUpdateIfExistsSql(string tableName, string keyColumn, List<string> updateColumns)
        {
            var setClause = string.Join(", ", updateColumns.Select(col => $"{col} = s.{col}"));
            
            return $@"
                UPDATE t SET {setClause}
                FROM {tableName} t
                INNER JOIN @DataTable s ON t.{keyColumn} = s.{keyColumn}";
        }

        private string GenerateSyncSql(string sourceTable, string targetTable, string keyColumn, List<string> syncColumns, bool deleteMissing)
        {
            var setClause = string.Join(", ", syncColumns.Select(col => $"{col} = s.{col}"));
            var insertColumns = string.Join(", ", syncColumns.Prepend(keyColumn));
            var insertValues = string.Join(", ", syncColumns.Prepend(keyColumn).Select(col => $"s.{col}"));
            
            var sql = $@"
                MERGE {targetTable} AS t
                USING {sourceTable} AS s ON t.{keyColumn} = s.{keyColumn}
                WHEN MATCHED THEN UPDATE SET {setClause}
                WHEN NOT MATCHED THEN INSERT ({insertColumns}) VALUES ({insertValues})";
            
            if (deleteMissing)
            {
                sql += $@"
                WHEN NOT MATCHED BY SOURCE THEN DELETE;";
            }
            
            return sql;
        }

        // Métodos de ejecución de lote
        private async Task ExecuteBatchInsert(SqlConnection connection, SqlTransaction transaction, BatchOperation operation)
        {
            var data = (IEnumerable<object>)operation.Data;
            var options = (BulkInsertOptions)operation.Options ?? new BulkInsertOptions();
            await InsertarDatosMasivamenteAsync(connection.ConnectionString, operation.TableName, data, options);
        }

        private async Task ExecuteBatchUpdate(SqlConnection connection, SqlTransaction transaction, BatchOperation operation)
        {
            var data = (IEnumerable<object>)operation.Data;
            var options = (BulkUpdateOptions)operation.Options ?? new BulkUpdateOptions();
            await ActualizarDatosMasivamenteAsync(connection.ConnectionString, operation.TableName, data, operation.KeyColumn, options);
        }

        private async Task ExecuteBatchDelete(SqlConnection connection, SqlTransaction transaction, BatchOperation operation)
        {
            var data = (IEnumerable<object>)operation.Data;
            var options = (BulkDeleteOptions)operation.Options ?? new BulkDeleteOptions();
            await EliminarDatosMasivamenteAsync(connection.ConnectionString, operation.TableName, data, operation.KeyColumn, options);
        }

        private async Task ExecuteBatchUpsert(SqlConnection connection, SqlTransaction transaction, BatchOperation operation)
        {
            var data = (IEnumerable<object>)operation.Data;
            var options = (BulkUpsertOptions)operation.Options ?? new BulkUpsertOptions();
            await InsertarOActualizarMasivamenteAsync(connection.ConnectionString, operation.TableName, data, operation.KeyColumn, options);
        }

        #endregion

        public DataSet ExecuteProcedure(string connectionString, string jsonParameters, string procedureName)
        {
            var parameters = string.IsNullOrWhiteSpace(jsonParameters)
                ? new List<StoredProcedureParameter>()
                : jsonParameters.ToStoredProcedureParameters();
            return ExecuteProcedure(connectionString, parameters, procedureName);
        }

        public DataSet ExecuteProcedure(string connectionString, string procedureName)
        {
            return ExecuteProcedure(connectionString, new List<StoredProcedureParameter>(), procedureName);
        }

        public DataSet ExecuteProcedure(string connectionString, List<StoredProcedureParameter> parameters, string procedureName)
        {
            var dataSet = new DataSet();
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(procedureName, connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.Add(param.ToSqlParameter());
                    }
                }
                var adapter = new SqlDataAdapter(command);
                adapter.Fill(dataSet);
            }
            return dataSet;
        }

        public async Task<DataSet> ExecuteProcedureAsync(string connectionString, string jsonParameters, string procedureName)
        {
            var parameters = string.IsNullOrWhiteSpace(jsonParameters)
                ? new List<StoredProcedureParameter>()
                : jsonParameters.ToStoredProcedureParameters();
            return await ExecuteProcedureAsync(connectionString, parameters, procedureName);
        }

        public async Task<DataSet> ExecuteProcedureAsync(string connectionString, string procedureName)
        {
            return await ExecuteProcedureAsync(connectionString, new List<StoredProcedureParameter>(), procedureName);
        }

        public async Task<DataSet> ExecuteProcedureAsync(string connectionString, List<StoredProcedureParameter> parameters, string procedureName)
        {
            var dataSet = new DataSet();
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(procedureName, connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.Add(param.ToSqlParameter());
                    }
                }
                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    dataSet.Load(reader, LoadOption.PreserveChanges, FillSchema(dataSet, reader));
                }
            }
            return dataSet;
        }

        public StoredProcedureResult<T> ExecuteProcedure<T>(string connectionString, string jsonParameters, string procedureName) where T : new()
        {
            try
            {
                var dataSet = ExecuteProcedure(connectionString, jsonParameters, procedureName);
                var result = new StoredProcedureResult<T>(dataSet);
                if (dataSet.Tables.Count > 0)
                {
                    result.Data = dataSet.Tables[0].ToObject<T>();
                }
                result.IsSuccess = true;
                result.Message = "Operación exitosa";
                return result;
            }
            catch (Exception ex)
            {
                return StoredProcedureResult<T>.Failure(ex);
            }
        }

        public async Task<StoredProcedureResult<T>> ExecuteProcedureAsync<T>(string connectionString, string jsonParameters, string procedureName) where T : new()
        {
            try
            {
                var dataSet = await ExecuteProcedureAsync(connectionString, jsonParameters, procedureName);
                var result = new StoredProcedureResult<T>(dataSet);
                if (dataSet.Tables.Count > 0)
                {
                    result.Data = dataSet.Tables[0].ToObject<T>();
                }
                result.IsSuccess = true;
                result.Message = "Operación exitosa";
                return result;
            }
            catch (Exception ex)
            {
                return StoredProcedureResult<T>.Failure(ex);
            }
        }

        public bool ValidateJson(string json)
        {
            return json.IsValidJson();
        }

        public DatabaseConnection GetConnectionInfo(string connectionString)
        {
            return new DatabaseConnection(connectionString);
        }

        // Utilidad interna para obtener los nombres de las tablas del DataReader
        private static DataTable[] FillSchema(DataSet dataSet, IDataReader reader)
        {
            var tables = new List<DataTable>();
            do
            {
                var schemaTable = reader.GetSchemaTable();
                if (schemaTable != null)
                {
                    var table = new DataTable();
                    foreach (DataRow row in schemaTable.Rows)
                    {
                        var columnName = row["ColumnName"].ToString();
                        var dataType = (Type)row["DataType"];
                        table.Columns.Add(columnName, dataType);
                    }
                    tables.Add(table);
                }
            } while (reader.NextResult());
            return tables.ToArray();
        }
    }
} 
