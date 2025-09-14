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
        /// Ejecuta un procedimiento almacenado y devuelve un resultado tipado como IEnumerable usando parámetros unificados
        /// </summary>
        public async Task<StoredProcedureResult<IEnumerable<TResult>>> EjecutarProcedimientoAsync<TResult>(StoredProcedureParameters parameters) where TResult : new()
        {
            try
            {
                var dataSet = new DataSet();
                using (var connection = new SqlConnection(parameters.ConnectionString))
                using (var command = new SqlCommand(parameters.ProcedureName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Si hay un modelo, obtener los parámetros del procedimiento y mapearlos
                    if (parameters.Model != null)
                    {
                        var dbParameters = await GetProcedureParametersAsync(parameters.ConnectionString, parameters.ProcedureName);
                        var modelDict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                        
                        foreach (var prop in parameters.Model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                        {
                            modelDict[prop.Name] = prop.GetValue(parameters.Model);
                        }

                        var sqlParameters = new List<SqlParameter>();

                        foreach (var dbParam in dbParameters)
                        {
                            if (modelDict.TryGetValue(dbParam.ParameterName.TrimStart('@'), out var value))
                            {
                                sqlParameters.Add(new SqlParameter(dbParam.ParameterName, dbParam.SqlDbType)
                                {
                                    Direction = dbParam.Direction,
                                    Size = dbParam.Size > 0 ? dbParam.Size : 0,
                                    Value = value ?? DBNull.Value
                                });
                            }
                        }

                        command.Parameters.AddRange(sqlParameters.ToArray());
                    }

                    // Añadir parámetros de paginación al comando si se proporcionan
                    if (parameters.ModelPaginacion?.PageNumber.HasValue == true) 
                        command.Parameters.AddWithValue("@PageNumber", parameters.ModelPaginacion.PageNumber.Value);
                    if (parameters.ModelPaginacion?.PageSize.HasValue == true) 
                        command.Parameters.AddWithValue("@PageSize", parameters.ModelPaginacion.PageSize.Value);

                    // Añadir parámetro de filtro si se proporciona
                    if (!string.IsNullOrEmpty(parameters.ModelPaginacion?.Filter)) 
                        command.Parameters.AddWithValue("@Filter", parameters.ModelPaginacion.Filter);

                    var adapter = new SqlDataAdapter(command);
                    adapter.Fill(dataSet);
                }

                var result = new StoredProcedureResult<IEnumerable<TResult>>(dataSet);
                if (dataSet.Tables.Count > 0)
                {
                    var table = dataSet.Tables[0];
                    result.Data = DataTableToList<TResult>(table);

                    // Extraer TotalRecords de la primera fila si existe y es un valor válido
                    if (table.Rows.Count > 0 && table.Columns.Contains("TotalRecords") && int.TryParse(table.Rows[0]["TotalRecords"].ToString(), out int totalRecords))
                    {
                        result.TotalRecords = totalRecords;
                    }
                    // Asignar los valores de paginación pasados al resultado
                    result.PageNumber = parameters.ModelPaginacion?.PageNumber;
                    result.PageSize = parameters.ModelPaginacion?.PageSize;
                }
                else
                {
                    result.Data = Enumerable.Empty<TResult>();
                    result.TotalRecords = 0; // Si no hay datos, TotalRecords es 0
                    result.PageNumber = parameters.ModelPaginacion?.PageNumber;
                    result.PageSize = parameters.ModelPaginacion?.PageSize;
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
        /// Ejecuta un procedimiento almacenado sin modelo y devuelve un resultado tipado como IEnumerable
        /// </summary>
        public async Task<StoredProcedureResult<IEnumerable<TResult>>> EjecutarProcedimientoAsync<TResult>(string connectionString, string procedureName, ModelPaginacion modelPaginacion = null) where TResult : new()
        {
            var parameters = new StoredProcedureParameters(connectionString, procedureName, null, modelPaginacion);
            return await EjecutarProcedimientoAsync<TResult>(parameters);
        }

        /// <summary>
        /// Realiza una actualización masiva de datos usando procedimientos almacenados
        /// </summary>
        public async Task<BulkOperationResult> ActualizarDatosMasivamenteAsync<TModel>(BulkUpdateParameters parameters) where TModel : class
        {
            var stopwatch = Stopwatch.StartNew();
            var dataList = parameters.Data?.Cast<TModel>().ToList() ?? new List<TModel>();
            
            if (!dataList.Any())
            {
                return BulkOperationResult.Success(0, stopwatch.ElapsedMilliseconds, parameters.BatchSize);
            }

            try
            {
                using (var connection = new SqlConnection(parameters.ConnectionString))
                {
                    await connection.OpenAsync();
                    
                    var totalRowsAffected = 0;
                    var batches = Chunk(dataList, parameters.BatchSize);
                    
                    foreach (var batch in batches)
                    {
                        var rowsAffected = await ProcessBulkUpdateBatchAsync<TModel>(connection, parameters, batch);
                        totalRowsAffected += rowsAffected;
                    }
                    
                    stopwatch.Stop();
                    return BulkOperationResult.Success(totalRowsAffected, stopwatch.ElapsedMilliseconds, parameters.BatchSize);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return BulkOperationResult.Failure(ex, $"Error en actualización masiva con procedimiento {parameters.ProcedureName}");
            }
        }

        /// <summary>
        /// Realiza una inserción masiva de datos usando procedimientos almacenados
        /// </summary>
        public async Task<BulkOperationResult> InsertarDatosMasivamenteAsync<TModel>(BulkInsertParameters parameters) where TModel : class
        {
            var stopwatch = Stopwatch.StartNew();
            var dataList = parameters.Data?.Cast<TModel>().ToList() ?? new List<TModel>();
            
            if (!dataList.Any())
            {
                return BulkOperationResult.Success(0, stopwatch.ElapsedMilliseconds, parameters.BatchSize);
            }

            try
            {
                using (var connection = new SqlConnection(parameters.ConnectionString))
                {
                    await connection.OpenAsync();
                    
                    var totalRowsAffected = 0;
                    var batches = Chunk(dataList, parameters.BatchSize);
                    
                    foreach (var batch in batches)
                    {
                        var rowsAffected = await ProcessBulkInsertBatchAsync<TModel>(connection, parameters, batch);
                        totalRowsAffected += rowsAffected;
                    }
                    
                    stopwatch.Stop();
                    return BulkOperationResult.Success(totalRowsAffected, stopwatch.ElapsedMilliseconds, parameters.BatchSize);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return BulkOperationResult.Failure(ex, $"Error en inserción masiva con procedimiento {parameters.ProcedureName}");
            }
        }

        #region Métodos Auxiliares

        // Procesar un lote de actualizaciones usando procedimiento almacenado
        private async Task<int> ProcessBulkUpdateBatchAsync<TModel>(SqlConnection connection, BulkUpdateParameters parameters, TModel[] batch) where TModel : class
        {
            if (!batch.Any()) return 0;

            try
            {
                using (var command = new SqlCommand(parameters.ProcedureName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Agregar parámetros del procedimiento almacenado
                    await AddBulkOperationParametersAsync(command, parameters, batch);

                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error ejecutando procedimiento {parameters.ProcedureName}: {ex.Message}", ex);
            }
        }

        // Procesar un lote de inserciones usando procedimiento almacenado
        private async Task<int> ProcessBulkInsertBatchAsync<TModel>(SqlConnection connection, BulkInsertParameters parameters, TModel[] batch) where TModel : class
        {
            if (!batch.Any()) return 0;

            try
            {
                using (var command = new SqlCommand(parameters.ProcedureName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Agregar parámetros del procedimiento almacenado
                    await AddBulkOperationParametersAsync(command, parameters, batch);

                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error ejecutando procedimiento {parameters.ProcedureName}: {ex.Message}", ex);
            }
        }

        // Agregar parámetros para operaciones masivas
        private async Task AddBulkOperationParametersAsync<TModel>(SqlCommand command, BulkOperationParameters parameters, TModel[] batch) where TModel : class
        {
            // Obtener los parámetros del procedimiento almacenado
            var dbParameters = await GetProcedureParametersAsync(command.Connection.ConnectionString, parameters.ProcedureName);
            var modelDict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            
            // Crear diccionario con las propiedades del modelo
            var properties = typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                modelDict[prop.Name] = prop;
            }

            // Agregar parámetros del procedimiento
            foreach (var dbParam in dbParameters)
            {
                var paramName = dbParam.ParameterName.TrimStart('@');
                
                // Buscar el parámetro en el modelo
                if (modelDict.TryGetValue(paramName, out var propertyInfo) && propertyInfo is PropertyInfo prop)
                {
                    // Si es una propiedad del modelo, agregar el valor del primer elemento del batch
                    var firstItem = batch.FirstOrDefault();
                    if (firstItem != null)
                    {
                        var value = prop.GetValue(firstItem);
                        command.Parameters.Add(new SqlParameter(dbParam.ParameterName, dbParam.SqlDbType)
                        {
                            Direction = dbParam.Direction,
                            Size = dbParam.Size > 0 ? dbParam.Size : 0,
                            Value = value ?? DBNull.Value
                        });
                    }
                }
                else if (paramName.Equals("BatchSize", StringComparison.OrdinalIgnoreCase))
                {
                    command.Parameters.Add(new SqlParameter(dbParam.ParameterName, dbParam.SqlDbType)
                    {
                        Direction = dbParam.Direction,
                        Value = parameters.BatchSize
                    });
                }
                else if (paramName.Equals("KeyColumn", StringComparison.OrdinalIgnoreCase) && parameters is BulkUpdateParameters updateParams)
                {
                    command.Parameters.Add(new SqlParameter(dbParam.ParameterName, dbParam.SqlDbType)
                    {
                        Direction = dbParam.Direction,
                        Value = updateParams.KeyColumn
                    });
                }
                else if (paramName.Equals("IgnoreIdentityColumns", StringComparison.OrdinalIgnoreCase) && parameters is BulkInsertParameters insertParams)
                {
                    command.Parameters.Add(new SqlParameter(dbParam.ParameterName, dbParam.SqlDbType)
                    {
                        Direction = dbParam.Direction,
                        Value = insertParams.IgnoreIdentityColumns
                    });
                }
                else if (parameters.AdditionalParameters != null)
                {
                    // Buscar en parámetros adicionales
                    var additionalProps = parameters.AdditionalParameters.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    var additionalProp = additionalProps.FirstOrDefault(p => 
                        string.Equals(p.Name, paramName, StringComparison.OrdinalIgnoreCase));
                    
                    if (additionalProp != null)
                    {
                        var value = additionalProp.GetValue(parameters.AdditionalParameters);
                        command.Parameters.Add(new SqlParameter(dbParam.ParameterName, dbParam.SqlDbType)
                        {
                            Direction = dbParam.Direction,
                            Size = dbParam.Size > 0 ? dbParam.Size : 0,
                            Value = value ?? DBNull.Value
                        });
                    }
                }
            }

            // Agregar parámetro para los datos del batch (como JSON)
            var dataParam = dbParameters.FirstOrDefault(p => 
                p.ParameterName.TrimStart('@').Equals("Data", StringComparison.OrdinalIgnoreCase) ||
                p.ParameterName.TrimStart('@').Equals("BatchData", StringComparison.OrdinalIgnoreCase));
            
            if (dataParam != null)
            {
                var dataJson = System.Text.Json.JsonSerializer.Serialize(batch);
                command.Parameters.Add(new SqlParameter(dataParam.ParameterName, SqlDbType.NVarChar)
                {
                    Direction = dataParam.Direction,
                    Size = dataJson.Length,
                    Value = dataJson
                });
            }
        }

        // Obtener información detallada de columnas de una tabla
        private async Task<Dictionary<string, ColumnInfo>> GetTableColumnInfoAsync(SqlConnection connection, string tableName)
        {
            var columnInfo = new Dictionary<string, ColumnInfo>(StringComparer.OrdinalIgnoreCase);
            var sql = @"
                SELECT 
                    COLUMN_NAME,
                    IS_IDENTITY,
                    IS_NULLABLE,
                    DATA_TYPE,
                    CHARACTER_MAXIMUM_LENGTH,
                    NUMERIC_PRECISION,
                    NUMERIC_SCALE
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = @TableName 
                ORDER BY ORDINAL_POSITION";

            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@TableName", tableName);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var columnName = reader["COLUMN_NAME"]?.ToString();
                        if (!string.IsNullOrEmpty(columnName))
                        {
                            columnInfo[columnName] = new ColumnInfo
                            {
                                Name = columnName,
                                IsIdentity = reader["IS_IDENTITY"]?.ToString() == "YES",
                                IsNullable = reader["IS_NULLABLE"]?.ToString() == "YES",
                                DataType = reader["DATA_TYPE"]?.ToString() ?? string.Empty,
                                MaxLength = reader["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value ? Convert.ToInt32(reader["CHARACTER_MAXIMUM_LENGTH"]) : (int?)null,
                                NumericPrecision = reader["NUMERIC_PRECISION"] != DBNull.Value ? Convert.ToInt32(reader["NUMERIC_PRECISION"]) : (int?)null,
                                NumericScale = reader["NUMERIC_SCALE"] != DBNull.Value ? Convert.ToInt32(reader["NUMERIC_SCALE"]) : (int?)null
                            };
                        }
                    }
                }
            }

            return columnInfo;
        }

        // Obtener columnas de una tabla
        private async Task<List<string>> GetTableColumnsAsync(SqlConnection connection, string tableName)
        {
            var columns = new List<string>();
            var sql = @"
                SELECT COLUMN_NAME 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = @TableName 
                ORDER BY ORDINAL_POSITION";

            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@TableName", tableName);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var columnName = reader["COLUMN_NAME"]?.ToString();
                        if (!string.IsNullOrEmpty(columnName))
                        {
                            columns.Add(columnName);
                        }
                    }
                }
            }

            return columns;
        }

        // Obtener nombre de columna para una propiedad
        private string? GetColumnName(PropertyInfo property, List<string> tableColumns)
        {
            // Verificar si hay mapeo explícito
            var columnMappingAttribute = property.GetCustomAttribute<ColumnMappingAttribute>();
            if (columnMappingAttribute != null)
            {
                return columnMappingAttribute.ColumnName;
            }

            // Mapeo automático
            var propertyName = property.Name;
            
            // 1. Coincidencia exacta
            var exactMatch = tableColumns.FirstOrDefault(c => 
                string.Equals(c, propertyName, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(exactMatch))
                return exactMatch;

            // 2. Coincidencia con guiones bajos
            var underscoreVersion = string.Join("_", 
                System.Text.RegularExpressions.Regex.Split(propertyName, @"(?<!^)(?=[A-Z])"));
            var underscoreMatch = tableColumns.FirstOrDefault(c => 
                string.Equals(c, underscoreVersion, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(underscoreMatch))
                return underscoreMatch;

            // 3. Coincidencia parcial
            var partialMatch = tableColumns.FirstOrDefault(c => 
                c.Contains(propertyName, StringComparison.OrdinalIgnoreCase) ||
                propertyName.Contains(c, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(partialMatch))
                return partialMatch;

            return null;
        }

        // Método Chunk para dividir colecciones en lotes (compatibilidad con .NET 9)
        private static IEnumerable<T[]> Chunk<T>(IEnumerable<T> source, int size)
        {
            if (size <= 0) throw new ArgumentException("El tamaño debe ser mayor que 0", nameof(size));
            
            var list = new List<T>();
            foreach (var item in source)
            {
                list.Add(item);
                if (list.Count == size)
                {
                    yield return list.ToArray();
                    list.Clear();
                }
            }
            
            if (list.Count > 0)
            {
                yield return list.ToArray();
            }
        }

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
            public string ParameterName { get; set; } = string.Empty;
            public SqlDbType SqlDbType { get; set; }
            public ParameterDirection Direction { get; set; }
            public int Size { get; set; }
        }

        // Clase auxiliar para información de columnas
        private class ColumnInfo
        {
            public string Name { get; set; } = string.Empty;
            public bool IsIdentity { get; set; }
            public bool IsNullable { get; set; }
            public string DataType { get; set; } = string.Empty;
            public int? MaxLength { get; set; }
            public int? NumericPrecision { get; set; }
            public int? NumericScale { get; set; }
        }

        #endregion
    }
} 


