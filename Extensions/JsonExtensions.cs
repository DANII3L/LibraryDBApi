using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using LibraryDBApi.Enums;
using LibraryDBApi.Models;

namespace LibraryDBApi.Extensions
{
    /// <summary>
    /// Extensiones para el manejo de JSON en procedimientos almacenados
    /// </summary>
    public static class JsonExtensions
    {
        /// <summary>
        /// Convierte un objeto a JSON
        /// </summary>
        /// <param name="obj">Objeto a convertir</param>
        /// <param name="options">Opciones de serialización</param>
        /// <returns>JSON como string</returns>
        public static string ToJson(this object obj, JsonSerializerOptions options = null)
        {
            if (obj == null)
                return "{}";

            options ??= new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            try
            {
                return JsonSerializer.Serialize(obj, options);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error al serializar objeto a JSON: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deserializa JSON a un objeto tipado
        /// </summary>
        /// <typeparam name="T">Tipo de objeto esperado</typeparam>
        /// <param name="json">JSON a deserializar</param>
        /// <param name="options">Opciones de deserialización</param>
        /// <returns>Objeto deserializado</returns>
        public static T FromJson<T>(this string json, JsonSerializerOptions options = null)
        {
            if (string.IsNullOrWhiteSpace(json))
                return default(T);

            options ??= new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            try
            {
                return JsonSerializer.Deserialize<T>(json, options);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error al deserializar JSON: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Convierte JSON a una lista de parámetros de procedimiento almacenado
        /// </summary>
        /// <param name="json">JSON con los parámetros</param>
        /// <returns>Lista de parámetros</returns>
        public static List<StoredProcedureParameter> ToStoredProcedureParameters(this string json)
        {
            var parameters = new List<StoredProcedureParameter>();

            if (string.IsNullOrWhiteSpace(json))
                return parameters;

            try
            {
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    foreach (JsonProperty property in document.RootElement.EnumerateObject())
                    {
                        var parameter = CreateParameterFromJsonElement(property.Name, property.Value);
                        if (parameter != null)
                        {
                            parameters.Add(parameter);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error al convertir JSON a parámetros: {ex.Message}", ex);
            }

            return parameters;
        }

        /// <summary>
        /// Convierte un DataTable a JSON
        /// </summary>
        /// <param name="dataTable">DataTable a convertir</param>
        /// <param name="options">Opciones de serialización</param>
        /// <returns>JSON como string</returns>
        public static string ToJson(this DataTable dataTable, JsonSerializerOptions options = null)
        {
            if (dataTable == null)
                return "[]";

            var rows = new List<Dictionary<string, object>>();

            foreach (DataRow row in dataTable.Rows)
            {
                var rowDict = new Dictionary<string, object>();
                foreach (DataColumn column in dataTable.Columns)
                {
                    var value = row[column];
                    rowDict[column.ColumnName] = value == DBNull.Value ? null : value;
                }
                rows.Add(rowDict);
            }

            return rows.ToJson(options);
        }

        /// <summary>
        /// Convierte un DataSet a JSON
        /// </summary>
        /// <param name="dataSet">DataSet a convertir</param>
        /// <param name="options">Opciones de serialización</param>
        /// <returns>JSON como string</returns>
        public static string ToJson(this DataSet dataSet, JsonSerializerOptions options = null)
        {
            if (dataSet == null)
                return "{}";

            var result = new Dictionary<string, object>();

            foreach (DataTable table in dataSet.Tables)
            {
                var tableName = string.IsNullOrWhiteSpace(table.TableName) ? $"Table{dataSet.Tables.IndexOf(table)}" : table.TableName;
                result[tableName] = table.ToJson(options);
            }

            return result.ToJson(options);
        }

        /// <summary>
        /// Valida si una cadena es JSON válido
        /// </summary>
        /// <param name="json">Cadena a validar</param>
        /// <returns>True si es JSON válido, False en caso contrario</returns>
        public static bool IsValidJson(this string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                using (JsonDocument.Parse(json))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Obtiene el valor de una propiedad JSON como string
        /// </summary>
        /// <param name="json">JSON a procesar</param>
        /// <param name="propertyName">Nombre de la propiedad</param>
        /// <returns>Valor de la propiedad o null si no existe</returns>
        public static string GetJsonProperty(this string json, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(propertyName))
                return null;

            try
            {
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    if (document.RootElement.TryGetProperty(propertyName, out JsonElement element))
                    {
                        return element.GetString();
                    }
                }
            }
            catch
            {
                // Ignorar errores y retornar null
            }

            return null;
        }

        /// <summary>
        /// Obtiene el valor de una propiedad JSON como tipo específico
        /// </summary>
        /// <typeparam name="T">Tipo esperado</typeparam>
        /// <param name="json">JSON a procesar</param>
        /// <param name="propertyName">Nombre de la propiedad</param>
        /// <returns>Valor de la propiedad o default(T) si no existe</returns>
        public static T GetJsonProperty<T>(this string json, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(propertyName))
                return default(T);

            try
            {
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    if (document.RootElement.TryGetProperty(propertyName, out JsonElement element))
                    {
                        return element.Deserialize<T>();
                    }
                }
            }
            catch
            {
                // Ignorar errores y retornar default
            }

            return default(T);
        }

        /// <summary>
        /// Crea un parámetro de procedimiento almacenado desde un elemento JSON
        /// </summary>
        /// <param name="name">Nombre del parámetro</param>
        /// <param name="element">Elemento JSON</param>
        /// <returns>Parámetro creado</returns>
        private static StoredProcedureParameter CreateParameterFromJsonElement(string name, JsonElement element)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var parameter = new StoredProcedureParameter
            {
                Name = name
            };

            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    parameter.Value = element.GetString();
                    parameter.ParameterType = ParameterType.String;
                    break;

                case JsonValueKind.Number:
                    if (element.TryGetInt32(out int intValue))
                    {
                        parameter.Value = intValue;
                        parameter.ParameterType = ParameterType.Integer;
                    }
                    else if (element.TryGetInt64(out long longValue))
                    {
                        parameter.Value = longValue;
                        parameter.ParameterType = ParameterType.Long;
                    }
                    else if (element.TryGetDecimal(out decimal decimalValue))
                    {
                        parameter.Value = decimalValue;
                        parameter.ParameterType = ParameterType.Decimal;
                    }
                    else
                    {
                        parameter.Value = element.GetDouble();
                        parameter.ParameterType = ParameterType.Decimal;
                    }
                    break;

                case JsonValueKind.True:
                case JsonValueKind.False:
                    parameter.Value = element.GetBoolean();
                    parameter.ParameterType = ParameterType.Boolean;
                    break;

                case JsonValueKind.Null:
                    parameter.Value = DBNull.Value;
                    parameter.ParameterType = ParameterType.String;
                    break;

                case JsonValueKind.Object:
                    parameter.Value = element.GetRawText();
                    parameter.ParameterType = ParameterType.String;
                    break;

                case JsonValueKind.Array:
                    parameter.Value = element.GetRawText();
                    parameter.ParameterType = ParameterType.String;
                    break;

                default:
                    parameter.Value = element.GetRawText();
                    parameter.ParameterType = ParameterType.String;
                    break;
            }

            return parameter;
        }

        /// <summary>
        /// Convierte un objeto a parámetros de procedimiento almacenado
        /// </summary>
        /// <param name="obj">Objeto a convertir</param>
        /// <returns>Lista de parámetros</returns>
        public static List<StoredProcedureParameter> ToStoredProcedureParameters(this object obj)
        {
            if (obj == null)
                return new List<StoredProcedureParameter>();

            var json = obj.ToJson();
            return json.ToStoredProcedureParameters();
        }

        /// <summary>
        /// Convierte parámetros de procedimiento almacenado a JSON
        /// </summary>
        /// <param name="parameters">Lista de parámetros</param>
        /// <param name="options">Opciones de serialización</param>
        /// <returns>JSON como string</returns>
        public static string ToJson(this List<StoredProcedureParameter> parameters, JsonSerializerOptions options = null)
        {
            if (parameters == null || parameters.Count == 0)
                return "{}";

            var dict = parameters.ToDictionary(p => p.Name, p => p.Value);
            return dict.ToJson(options);
        }
    }
} 