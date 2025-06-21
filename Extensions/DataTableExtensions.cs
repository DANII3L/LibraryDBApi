using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace LibraryDBApi.Extensions
{
    /// <summary>
    /// Extensiones para el manejo de DataTable
    /// </summary>
    public static class DataTableExtensions
    {
        /// <summary>
        /// Convierte un DataTable a una lista de objetos tipados
        /// </summary>
        /// <typeparam name="T">Tipo de objeto destino</typeparam>
        /// <param name="dataTable">DataTable a convertir</param>
        /// <returns>Lista de objetos tipados</returns>
        public static List<T> ToList<T>(this DataTable dataTable) where T : new()
        {
            if (dataTable == null || dataTable.Rows.Count == 0)
                return new List<T>();

            var list = new List<T>();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (DataRow row in dataTable.Rows)
            {
                var item = new T();
                foreach (var property in properties)
                {
                    if (dataTable.Columns.Contains(property.Name))
                    {
                        var value = row[property.Name];
                        if (value != DBNull.Value)
                        {
                            try
                            {
                                var convertedValue = Convert.ChangeType(value, property.PropertyType);
                                property.SetValue(item, convertedValue);
                            }
                            catch
                            {
                                // Ignorar errores de conversión
                            }
                        }
                    }
                }
                list.Add(item);
            }

            return list;
        }

        /// <summary>
        /// Convierte un DataTable a un objeto tipado (primera fila)
        /// </summary>
        /// <typeparam name="T">Tipo de objeto destino</typeparam>
        /// <param name="dataTable">DataTable a convertir</param>
        /// <returns>Objeto tipado o default(T) si no hay datos</returns>
        public static T ToObject<T>(this DataTable dataTable) where T : new()
        {
            if (dataTable == null || dataTable.Rows.Count == 0)
                return default(T);

            var list = dataTable.ToList<T>();
            return list.FirstOrDefault();
        }

        /// <summary>
        /// Convierte un DataTable a un diccionario de la primera fila
        /// </summary>
        /// <param name="dataTable">DataTable a convertir</param>
        /// <returns>Diccionario con los valores de la primera fila</returns>
        public static Dictionary<string, object> ToDictionary(this DataTable dataTable)
        {
            if (dataTable == null || dataTable.Rows.Count == 0)
                return new Dictionary<string, object>();

            var dict = new Dictionary<string, object>();
            var row = dataTable.Rows[0];

            foreach (DataColumn column in dataTable.Columns)
            {
                var value = row[column];
                dict[column.ColumnName] = value == DBNull.Value ? null : value;
            }

            return dict;
        }

        /// <summary>
        /// Convierte un DataTable a una lista de diccionarios
        /// </summary>
        /// <param name="dataTable">DataTable a convertir</param>
        /// <returns>Lista de diccionarios</returns>
        public static List<Dictionary<string, object>> ToDictionaryList(this DataTable dataTable)
        {
            if (dataTable == null || dataTable.Rows.Count == 0)
                return new List<Dictionary<string, object>>();

            var list = new List<Dictionary<string, object>>();

            foreach (DataRow row in dataTable.Rows)
            {
                var dict = new Dictionary<string, object>();
                foreach (DataColumn column in dataTable.Columns)
                {
                    var value = row[column];
                    dict[column.ColumnName] = value == DBNull.Value ? null : value;
                }
                list.Add(dict);
            }

            return list;
        }

        /// <summary>
        /// Filtra un DataTable por una condición
        /// </summary>
        /// <param name="dataTable">DataTable a filtrar</param>
        /// <param name="filterExpression">Expresión de filtro</param>
        /// <returns>DataTable filtrado</returns>
        public static DataTable Filter(this DataTable dataTable, string filterExpression)
        {
            if (dataTable == null)
                return null;

            if (string.IsNullOrWhiteSpace(filterExpression))
                return dataTable.Copy();

            try
            {
                var rows = dataTable.Select(filterExpression);
                return rows.Length > 0 ? rows.CopyToDataTable() : dataTable.Clone();
            }
            catch
            {
                return dataTable.Clone();
            }
        }

        /// <summary>
        /// Ordena un DataTable por una columna
        /// </summary>
        /// <param name="dataTable">DataTable a ordenar</param>
        /// <param name="sortExpression">Expresión de ordenamiento</param>
        /// <returns>DataTable ordenado</returns>
        public static DataTable Sort(this DataTable dataTable, string sortExpression)
        {
            if (dataTable == null)
                return null;

            if (string.IsNullOrWhiteSpace(sortExpression))
                return dataTable.Copy();

            try
            {
                var rows = dataTable.Select("", sortExpression);
                return rows.Length > 0 ? rows.CopyToDataTable() : dataTable.Clone();
            }
            catch
            {
                return dataTable.Copy();
            }
        }

        /// <summary>
        /// Obtiene valores únicos de una columna
        /// </summary>
        /// <param name="dataTable">DataTable</param>
        /// <param name="columnName">Nombre de la columna</param>
        /// <returns>Lista de valores únicos</returns>
        public static List<object> GetUniqueValues(this DataTable dataTable, string columnName)
        {
            if (dataTable == null || !dataTable.Columns.Contains(columnName))
                return new List<object>();

            return dataTable.AsEnumerable()
                .Select(row => row[columnName])
                .Where(value => value != DBNull.Value)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Obtiene el valor máximo de una columna
        /// </summary>
        /// <typeparam name="T">Tipo de valor esperado</typeparam>
        /// <param name="dataTable">DataTable</param>
        /// <param name="columnName">Nombre de la columna</param>
        /// <returns>Valor máximo o default(T)</returns>
        public static T GetMaxValue<T>(this DataTable dataTable, string columnName)
        {
            if (dataTable == null || !dataTable.Columns.Contains(columnName))
                return default(T);

            try
            {
                var values = dataTable.AsEnumerable()
                    .Select(row => row[columnName])
                    .Where(value => value != DBNull.Value)
                    .Cast<T>();

                return values.Any() ? values.Max() : default(T);
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// Obtiene el valor mínimo de una columna
        /// </summary>
        /// <typeparam name="T">Tipo de valor esperado</typeparam>
        /// <param name="dataTable">DataTable</param>
        /// <param name="columnName">Nombre de la columna</param>
        /// <returns>Valor mínimo o default(T)</returns>
        public static T GetMinValue<T>(this DataTable dataTable, string columnName)
        {
            if (dataTable == null || !dataTable.Columns.Contains(columnName))
                return default(T);

            try
            {
                var values = dataTable.AsEnumerable()
                    .Select(row => row[columnName])
                    .Where(value => value != DBNull.Value)
                    .Cast<T>();

                return values.Any() ? values.Min() : default(T);
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// Obtiene el promedio de una columna numérica
        /// </summary>
        /// <param name="dataTable">DataTable</param>
        /// <param name="columnName">Nombre de la columna</param>
        /// <returns>Promedio o 0 si no hay datos</returns>
        public static double GetAverage(this DataTable dataTable, string columnName)
        {
            if (dataTable == null || !dataTable.Columns.Contains(columnName))
                return 0;

            try
            {
                var values = dataTable.AsEnumerable()
                    .Select(row => row[columnName])
                    .Where(value => value != DBNull.Value)
                    .Select(value => Convert.ToDouble(value));

                return values.Any() ? values.Average() : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Obtiene la suma de una columna numérica
        /// </summary>
        /// <param name="dataTable">DataTable</param>
        /// <param name="columnName">Nombre de la columna</param>
        /// <returns>Suma o 0 si no hay datos</returns>
        public static double GetSum(this DataTable dataTable, string columnName)
        {
            if (dataTable == null || !dataTable.Columns.Contains(columnName))
                return 0;

            try
            {
                var values = dataTable.AsEnumerable()
                    .Select(row => row[columnName])
                    .Where(value => value != DBNull.Value)
                    .Select(value => Convert.ToDouble(value));

                return values.Any() ? values.Sum() : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Obtiene el conteo de filas no nulas en una columna
        /// </summary>
        /// <param name="dataTable">DataTable</param>
        /// <param name="columnName">Nombre de la columna</param>
        /// <returns>Conteo de valores no nulos</returns>
        public static int GetCount(this DataTable dataTable, string columnName)
        {
            if (dataTable == null || !dataTable.Columns.Contains(columnName))
                return 0;

            return dataTable.AsEnumerable()
                .Count(row => row[columnName] != DBNull.Value);
        }

        /// <summary>
        /// Verifica si un DataTable tiene datos
        /// </summary>
        /// <param name="dataTable">DataTable a verificar</param>
        /// <returns>True si tiene datos, False en caso contrario</returns>
        public static bool HasData(this DataTable dataTable)
        {
            return dataTable != null && dataTable.Rows.Count > 0;
        }

        /// <summary>
        /// Obtiene el número de filas en un DataTable
        /// </summary>
        /// <param name="dataTable">DataTable</param>
        /// <returns>Número de filas</returns>
        public static int RowCount(this DataTable dataTable)
        {
            return dataTable?.Rows.Count ?? 0;
        }

        /// <summary>
        /// Obtiene el número de columnas en un DataTable
        /// </summary>
        /// <param name="dataTable">DataTable</param>
        /// <returns>Número de columnas</returns>
        public static int ColumnCount(this DataTable dataTable)
        {
            return dataTable?.Columns.Count ?? 0;
        }

        /// <summary>
        /// Obtiene los nombres de las columnas
        /// </summary>
        /// <param name="dataTable">DataTable</param>
        /// <returns>Lista de nombres de columnas</returns>
        public static List<string> GetColumnNames(this DataTable dataTable)
        {
            if (dataTable == null)
                return new List<string>();

            return dataTable.Columns.Cast<DataColumn>()
                .Select(column => column.ColumnName)
                .ToList();
        }

        /// <summary>
        /// Verifica si una columna existe en el DataTable
        /// </summary>
        /// <param name="dataTable">DataTable</param>
        /// <param name="columnName">Nombre de la columna</param>
        /// <returns>True si existe, False en caso contrario</returns>
        public static bool HasColumn(this DataTable dataTable, string columnName)
        {
            return dataTable?.Columns.Contains(columnName) ?? false;
        }
    }
} 