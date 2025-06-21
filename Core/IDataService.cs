using System.Collections.Generic;
using System.Threading.Tasks;
using LibraryDBApi.Models;

namespace LibraryDBApi.Core
{
    /// <summary>
    /// Interfaz principal para el servicio de datos que maneja procedimientos almacenados de forma innovadora
    /// </summary>
    public interface IDataService
    {
        /// <summary>
        /// Ejecuta un procedimiento almacenado y devuelve un resultado tipado como IEnumerable
        /// </summary>
        Task<StoredProcedureResult<IEnumerable<TResult>>> EjecutarProcedimientoAsync<TResult>(string connectionString, string procedureName) where TResult : new();

        /// <summary>
        /// Ejecuta un procedimiento almacenado con parámetros y devuelve un resultado tipado como IEnumerable (inferencia automática del modelo)
        /// </summary>
        Task<StoredProcedureResult<IEnumerable<TResult>>> EjecutarProcedimientoAsync<TResult>(string connectionString, string procedureName, object model) where TResult : new();

        #region Operaciones Masivas Innovadoras

        /// <summary>
        /// Inserta datos masivamente usando SqlBulkCopy para máxima velocidad
        /// </summary>
        Task<BulkOperationResult> InsertarDatosMasivamenteAsync<T>(string connectionString, string tableName, IEnumerable<T> data, BulkInsertOptions options = null);

        /// <summary>
        /// Actualiza datos masivamente usando Table-Valued Parameters
        /// </summary>
        Task<BulkOperationResult> ActualizarDatosMasivamenteAsync<T>(string connectionString, string tableName, IEnumerable<T> data, string keyColumn, BulkUpdateOptions options = null);

        /// <summary>
        /// Elimina datos masivamente de forma optimizada
        /// </summary>
        Task<BulkOperationResult> EliminarDatosMasivamenteAsync<T>(string connectionString, string tableName, IEnumerable<T> data, string keyColumn, BulkDeleteOptions options = null);

        /// <summary>
        /// Realiza operaciones de inserción/actualización masivas (Upsert)
        /// </summary>
        Task<BulkOperationResult> InsertarOActualizarMasivamenteAsync<T>(string connectionString, string tableName, IEnumerable<T> data, string keyColumn, BulkUpsertOptions options = null);

        /// <summary>
        /// Ejecuta múltiples operaciones en lote con transacción
        /// </summary>
        Task<BatchOperationResult> EjecutarOperacionesEnLoteAsync(string connectionString, IEnumerable<BatchOperation> operations);

        /// <summary>
        /// Sincroniza datos entre tablas de forma masiva
        /// </summary>
        Task<BulkOperationResult> SincronizarDatosMasivamenteAsync<T>(string connectionString, string sourceTable, string targetTable, string keyColumn, BulkSyncOptions options = null);

        #endregion
    }
} 