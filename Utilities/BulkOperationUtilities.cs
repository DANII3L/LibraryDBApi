using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibraryDBApi.Models;

namespace LibraryDBApi.Utilities
{
    /// <summary>
    /// Utilidades especializadas para operaciones masivas optimizadas
    /// </summary>
    public static class BulkOperationUtilities
    {
        /// <summary>
        /// Genera tipos de tabla personalizados para operaciones masivas
        /// </summary>
        public static async Task<bool> CrearTipoDeTablaAsync(string connectionString, string typeName, Type modelType, bool dropIfExists = true)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    if (dropIfExists)
                    {
                        var dropSql = $"IF TYPE_ID('{typeName}') IS NOT NULL DROP TYPE {typeName}";
                        using (var command = new SqlCommand(dropSql, connection))
                        {
                            await command.ExecuteNonQueryAsync();
                        }
                    }

                    var createSql = GenerateTableTypeSql(typeName, modelType);
                    using (var command = new SqlCommand(createSql, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }

                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Genera SQL para crear tipo de tabla
        /// </summary>
        private static string GenerateTableTypeSql(string typeName, Type modelType)
        {
            var properties = modelType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var columns = new List<string>();

            foreach (var prop in properties)
            {
                var sqlType = GetSqlTypeFromClrType(prop.PropertyType);
                var nullable = IsNullableType(prop.PropertyType) ? "NULL" : "NOT NULL";
                columns.Add($"{prop.Name} {sqlType} {nullable}");
            }

            var columnsSql = string.Join(",\n    ", columns);
            return $@"
CREATE TYPE {typeName} AS TABLE
(
    {columnsSql}
)";
        }

        /// <summary>
        /// Mapea tipos CLR a tipos SQL
        /// </summary>
        private static string GetSqlTypeFromClrType(Type clrType)
        {
            if (clrType == typeof(int)) return "INT";
            if (clrType == typeof(long)) return "BIGINT";
            if (clrType == typeof(decimal)) return "DECIMAL(18,2)";
            if (clrType == typeof(double)) return "FLOAT";
            if (clrType == typeof(float)) return "REAL";
            if (clrType == typeof(string)) return "NVARCHAR(MAX)";
            if (clrType == typeof(DateTime)) return "DATETIME2";
            if (clrType == typeof(bool)) return "BIT";
            if (clrType == typeof(Guid)) return "UNIQUEIDENTIFIER";
            if (clrType == typeof(byte[])) return "VARBINARY(MAX)";
            if (clrType == typeof(TimeSpan)) return "TIME";
            if (clrType == typeof(DateTimeOffset)) return "DATETIMEOFFSET";

            // Tipos nullables
            var underlyingType = Nullable.GetUnderlyingType(clrType);
            if (underlyingType != null)
            {
                return GetSqlTypeFromClrType(underlyingType);
            }

            return "NVARCHAR(MAX)";
        }

        /// <summary>
        /// Verifica si un tipo es nullable
        /// </summary>
        private static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        /// <summary>
        /// Optimiza el tamaño de lote basado en el rendimiento
        /// </summary>
        public static int OptimizarTamañoDeLote(int dataCount, int initialBatchSize = 1000)
        {
            if (dataCount <= 1000) return Math.Min(dataCount, 500);
            if (dataCount <= 10000) return 1000;
            if (dataCount <= 100000) return 2000;
            if (dataCount <= 1000000) return 5000;
            return 10000;
        }

        /// <summary>
        /// Divide datos en lotes optimizados
        /// </summary>
        public static IEnumerable<List<T>> DividirDatosEnLotes<T>(IEnumerable<T> data, int batchSize)
        {
            var batch = new List<T>();
            foreach (var item in data)
            {
                batch.Add(item);
                if (batch.Count >= batchSize)
                {
                    yield return batch;
                    batch = new List<T>();
                }
            }
            if (batch.Any())
            {
                yield return batch;
            }
        }

        /// <summary>
        /// Genera índices temporales para operaciones masivas
        /// </summary>
        public static async Task<bool> CrearIndiceTemporalAsync(string connectionString, string tableName, string columnName, string indexName = null)
        {
            try
            {
                indexName ??= $"IX_{tableName}_{columnName}_Temp_{Guid.NewGuid():N}";
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    var sql = $"CREATE NONCLUSTERED INDEX {indexName} ON {tableName} ({columnName})";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Elimina índices temporales
        /// </summary>
        public static async Task<bool> EliminarIndiceTemporalAsync(string connectionString, string tableName, string indexName)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    var sql = $"DROP INDEX {indexName} ON {tableName}";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Deshabilita índices para operaciones masivas
        /// </summary>
        public static async Task<List<string>> DeshabilitarIndicesAsync(string connectionString, string tableName)
        {
            var disabledIndexes = new List<string>();
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    var sql = $@"
                        SELECT i.name 
                        FROM sys.indexes i 
                        WHERE i.object_id = OBJECT_ID('{tableName}') 
                        AND i.type > 0 
                        AND i.is_disabled = 0";

                    using (var command = new SqlCommand(sql, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var indexName = reader["name"].ToString();
                            disabledIndexes.Add(indexName);
                        }
                    }

                    foreach (var indexName in disabledIndexes)
                    {
                        var disableSql = $"ALTER INDEX {indexName} ON {tableName} DISABLE";
                        using (var command = new SqlCommand(disableSql, connection))
                        {
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
            catch
            {
                // Si falla, intentar re-habilitar los que se deshabilitaron
                await ReconstruirIndicesAsync(connectionString, tableName, disabledIndexes);
            }

            return disabledIndexes;
        }

        /// <summary>
        /// Reconstruye índices después de operaciones masivas
        /// </summary>
        public static async Task<bool> ReconstruirIndicesAsync(string connectionString, string tableName, List<string> indexNames = null)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    if (indexNames == null || !indexNames.Any())
                    {
                        // Reconstruir todos los índices
                        var sql = $"ALTER INDEX ALL ON {tableName} REBUILD";
                        using (var command = new SqlCommand(sql, connection))
                        {
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                    else
                    {
                        // Reconstruir índices específicos
                        foreach (var indexName in indexNames)
                        {
                            var sql = $"ALTER INDEX {indexName} ON {tableName} REBUILD";
                            using (var command = new SqlCommand(sql, connection))
                            {
                                await command.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Optimiza configuración de base de datos para operaciones masivas
        /// </summary>
        public static async Task<BulkOperationOptimizationResult> OptimizarBaseDeDatosParaOperacionesMasivasAsync(string connectionString)
        {
            var result = new BulkOperationOptimizationResult();
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Configurar opciones de sesión para mejor rendimiento
                    var optimizations = new[]
                    {
                        "SET ARITHABORT ON",
                        "SET NUMERIC_ROUNDABORT OFF",
                        "SET CONCAT_NULL_YIELDS_NULL ON",
                        "SET ANSI_WARNINGS ON",
                        "SET ANSI_PADDING ON",
                        "SET ANSI_NULLS ON",
                        "SET QUOTED_IDENTIFIER ON"
                    };

                    foreach (var optimization in optimizations)
                    {
                        using (var command = new SqlCommand(optimization, connection))
                        {
                            await command.ExecuteNonQueryAsync();
                        }
                    }

                    result.IsSuccess = true;
                    result.Message = "Base de datos optimizada para operaciones masivas";
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = $"Error al optimizar: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Genera estadísticas de rendimiento para operaciones masivas
        /// </summary>
        public static async Task<BulkOperationPerformanceStats> ObtenerEstadisticasDeRendimientoAsync(string connectionString, string tableName)
        {
            var stats = new BulkOperationPerformanceStats();
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Obtener estadísticas de la tabla
                    var sql = $@"
                        SELECT 
                            p.rows as RowCount,
                            p.data_pages as DataPages,
                            p.used_pages as UsedPages,
                            p.reserved_pages as ReservedPages
                        FROM sys.partitions p
                        WHERE p.object_id = OBJECT_ID('{tableName}')
                        AND p.index_id <= 1";

                    using (var command = new SqlCommand(sql, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            stats.RowCount = Convert.ToInt64(reader["RowCount"]);
                            stats.DataPages = Convert.ToInt32(reader["DataPages"]);
                            stats.UsedPages = Convert.ToInt32(reader["UsedPages"]);
                            stats.ReservedPages = Convert.ToInt32(reader["ReservedPages"]);
                        }
                    }

                    // Obtener información de índices
                    sql = $@"
                        SELECT 
                            COUNT(*) as IndexCount,
                            SUM(CASE WHEN i.type = 1 THEN 1 ELSE 0 END) as ClusteredIndexCount,
                            SUM(CASE WHEN i.type = 2 THEN 1 ELSE 0 END) as NonClusteredIndexCount
                        FROM sys.indexes i
                        WHERE i.object_id = OBJECT_ID('{tableName}')";

                    using (var command = new SqlCommand(sql, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            stats.IndexCount = Convert.ToInt32(reader["IndexCount"]);
                            stats.ClusteredIndexCount = Convert.ToInt32(reader["ClusteredIndexCount"]);
                            stats.NonClusteredIndexCount = Convert.ToInt32(reader["NonClusteredIndexCount"]);
                        }
                    }
                }

                stats.IsSuccess = true;
            }
            catch (Exception ex)
            {
                stats.IsSuccess = false;
                stats.ErrorMessage = ex.Message;
            }

            return stats;
        }

        /// <summary>
        /// Genera recomendaciones de optimización
        /// </summary>
        public static BulkOperationRecommendations GenerarRecomendaciones(BulkOperationPerformanceStats stats, int dataCount)
        {
            var recommendations = new BulkOperationRecommendations();

            // Recomendaciones basadas en el tamaño de datos
            if (dataCount > 100000)
            {
                recommendations.BatchSize = 5000;
                recommendations.DisableIndexes = true;
                recommendations.UseTransaction = false;
            }
            else if (dataCount > 10000)
            {
                recommendations.BatchSize = 2000;
                recommendations.DisableIndexes = true;
                recommendations.UseTransaction = true;
            }
            else
            {
                recommendations.BatchSize = 1000;
                recommendations.DisableIndexes = false;
                recommendations.UseTransaction = true;
            }

            // Recomendaciones basadas en estadísticas de tabla
            if (stats.IndexCount > 5)
            {
                recommendations.DisableIndexes = true;
                recommendations.RecommendationMessages.Add("Muchos índices detectados. Considere deshabilitarlos temporalmente.");
            }

            if (stats.RowCount > 1000000)
            {
                recommendations.BatchSize = Math.Max(recommendations.BatchSize, 10000);
                recommendations.RecommendationMessages.Add("Tabla grande detectada. Use lotes más grandes.");
            }

            return recommendations;
        }
    }

    /// <summary>
    /// Resultado de optimización de base de datos
    /// </summary>
    public class BulkOperationOptimizationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public long OptimizationTimeMs { get; set; }
    }

    /// <summary>
    /// Estadísticas de rendimiento para operaciones masivas
    /// </summary>
    public class BulkOperationPerformanceStats
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public long RowCount { get; set; }
        public int DataPages { get; set; }
        public int UsedPages { get; set; }
        public int ReservedPages { get; set; }
        public int IndexCount { get; set; }
        public int ClusteredIndexCount { get; set; }
        public int NonClusteredIndexCount { get; set; }
    }

    /// <summary>
    /// Recomendaciones de optimización para operaciones masivas
    /// </summary>
    public class BulkOperationRecommendations
    {
        public int BatchSize { get; set; } = 1000;
        public bool DisableIndexes { get; set; } = false;
        public bool UseTransaction { get; set; } = true;
        public bool UseBulkCopy { get; set; } = true;
        public List<string> RecommendationMessages { get; set; } = new List<string>();
    }
} 