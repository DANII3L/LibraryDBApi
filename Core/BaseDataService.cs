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
    }
} 
