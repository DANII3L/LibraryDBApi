using System;
using System.Collections.Generic;
using System.Data;

namespace LibraryDBApi.Models
{
    /// <summary>
    /// Resultado de una operación masiva
    /// </summary>
    public class BulkOperationResult
    {
        /// <summary>
        /// Indica si la operación fue exitosa
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Mensaje de resultado
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Número de filas afectadas
        /// </summary>
        public int RowsAffected { get; set; }

        /// <summary>
        /// Tiempo de ejecución en milisegundos
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Tamaño del lote procesado
        /// </summary>
        public int BatchSize { get; set; }

        /// <summary>
        /// Excepción capturada (si aplica)
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Estadísticas detalladas de la operación
        /// </summary>
        public BulkOperationStats Stats { get; set; }

        public BulkOperationResult()
        {
            Stats = new BulkOperationStats();
        }

        public static BulkOperationResult Success(int rowsAffected, long executionTimeMs, int batchSize = 0)
        {
            return new BulkOperationResult
            {
                IsSuccess = true,
                Message = "Operación masiva exitosa",
                RowsAffected = rowsAffected,
                ExecutionTimeMs = executionTimeMs,
                BatchSize = batchSize
            };
        }

        public static BulkOperationResult Failure(Exception ex, string message = null)
        {
            return new BulkOperationResult
            {
                IsSuccess = false,
                Message = message ?? ex?.Message ?? "Error en operación masiva",
                Exception = ex
            };
        }
    }

    /// <summary>
    /// Estadísticas detalladas de operaciones masivas
    /// </summary>
    public class BulkOperationStats
    {
        /// <summary>
        /// Filas insertadas
        /// </summary>
        public int RowsInserted { get; set; }

        /// <summary>
        /// Filas actualizadas
        /// </summary>
        public int RowsUpdated { get; set; }

        /// <summary>
        /// Filas eliminadas
        /// </summary>
        public int RowsDeleted { get; set; }

        /// <summary>
        /// Filas ignoradas (duplicadas, etc.)
        /// </summary>
        public int RowsIgnored { get; set; }

        /// <summary>
        /// Errores encontrados
        /// </summary>
        public int Errors { get; set; }

        /// <summary>
        /// Tiempo de preparación en ms
        /// </summary>
        public long PreparationTimeMs { get; set; }

        /// <summary>
        /// Tiempo de transferencia en ms
        /// </summary>
        public long TransferTimeMs { get; set; }

        /// <summary>
        /// Tiempo de procesamiento en ms
        /// </summary>
        public long ProcessingTimeMs { get; set; }
    }

    /// <summary>
    /// Opciones para inserción masiva
    /// </summary>
    public class BulkInsertOptions
    {
        /// <summary>
        /// Tamaño del lote (default: 1000)
        /// </summary>
        public int BatchSize { get; set; } = 1000;

        /// <summary>
        /// Tiempo de espera en segundos (default: 30)
        /// </summary>
        public int Timeout { get; set; } = 30;

        /// <summary>
        /// Indica si mantener la identidad (default: false)
        /// </summary>
        public bool KeepIdentity { get; set; } = false;

        /// <summary>
        /// Indica si verificar restricciones (default: true)
        /// </summary>
        public bool CheckConstraints { get; set; } = true;

        /// <summary>
        /// Indica si mantener nulls (default: true)
        /// </summary>
        public bool KeepNulls { get; set; } = true;

        /// <summary>
        /// Indica si usar trigger (default: false)
        /// </summary>
        public bool FireTriggers { get; set; } = false;

        /// <summary>
        /// Mapeo personalizado de columnas
        /// </summary>
        public Dictionary<string, string> ColumnMappings { get; set; }

        /// <summary>
        /// Columnas a excluir
        /// </summary>
        public List<string> ExcludeColumns { get; set; }

        public BulkInsertOptions()
        {
            ColumnMappings = new Dictionary<string, string>();
            ExcludeColumns = new List<string>();
        }
    }

    /// <summary>
    /// Opciones para actualización masiva
    /// </summary>
    public class BulkUpdateOptions
    {
        /// <summary>
        /// Tamaño del lote (default: 1000)
        /// </summary>
        public int BatchSize { get; set; } = 1000;

        /// <summary>
        /// Tiempo de espera en segundos (default: 30)
        /// </summary>
        public int Timeout { get; set; } = 30;

        /// <summary>
        /// Columnas a actualizar (si es null, actualiza todas excepto la clave)
        /// </summary>
        public List<string> UpdateColumns { get; set; }

        /// <summary>
        /// Indica si usar transacción (default: true)
        /// </summary>
        public bool UseTransaction { get; set; } = true;

        /// <summary>
        /// Indica si continuar en caso de error (default: false)
        /// </summary>
        public bool ContinueOnError { get; set; } = false;

        public BulkUpdateOptions()
        {
            UpdateColumns = new List<string>();
        }
    }

    /// <summary>
    /// Opciones para eliminación masiva
    /// </summary>
    public class BulkDeleteOptions
    {
        /// <summary>
        /// Tamaño del lote (default: 1000)
        /// </summary>
        public int BatchSize { get; set; } = 1000;

        /// <summary>
        /// Tiempo de espera en segundos (default: 30)
        /// </summary>
        public int Timeout { get; set; } = 30;

        /// <summary>
        /// Indica si usar transacción (default: true)
        /// </summary>
        public bool UseTransaction { get; set; } = true;

        /// <summary>
        /// Indica si continuar en caso de error (default: false)
        /// </summary>
        public bool ContinueOnError { get; set; } = false;

        /// <summary>
        /// Indica si verificar existencia antes de eliminar (default: true)
        /// </summary>
        public bool CheckExistence { get; set; } = true;
    }

    /// <summary>
    /// Opciones para operaciones Upsert masivas
    /// </summary>
    public class BulkUpsertOptions
    {
        /// <summary>
        /// Tamaño del lote (default: 1000)
        /// </summary>
        public int BatchSize { get; set; } = 1000;

        /// <summary>
        /// Tiempo de espera en segundos (default: 30)
        /// </summary>
        public int Timeout { get; set; } = 30;

        /// <summary>
        /// Columnas a actualizar (si es null, actualiza todas excepto la clave)
        /// </summary>
        public List<string> UpdateColumns { get; set; }

        /// <summary>
        /// Indica si usar transacción (default: true)
        /// </summary>
        public bool UseTransaction { get; set; } = true;

        /// <summary>
        /// Estrategia de Upsert (default: Merge)
        /// </summary>
        public UpsertStrategy Strategy { get; set; } = UpsertStrategy.Merge;

        public BulkUpsertOptions()
        {
            UpdateColumns = new List<string>();
        }
    }

    /// <summary>
    /// Estrategias de Upsert
    /// </summary>
    public enum UpsertStrategy
    {
        /// <summary>
        /// Usar MERGE statement
        /// </summary>
        Merge,

        /// <summary>
        /// Usar INSERT con IF EXISTS
        /// </summary>
        InsertIfNotExists,

        /// <summary>
        /// Usar UPDATE con IF EXISTS
        /// </summary>
        UpdateIfExists
    }

    /// <summary>
    /// Opciones para sincronización masiva
    /// </summary>
    public class BulkSyncOptions
    {
        /// <summary>
        /// Tamaño del lote (default: 1000)
        /// </summary>
        public int BatchSize { get; set; } = 1000;

        /// <summary>
        /// Tiempo de espera en segundos (default: 30)
        /// </summary>
        public int Timeout { get; set; } = 30;

        /// <summary>
        /// Indica si eliminar registros que no existen en origen (default: false)
        /// </summary>
        public bool DeleteMissing { get; set; } = false;

        /// <summary>
        /// Indica si usar transacción (default: true)
        /// </summary>
        public bool UseTransaction { get; set; } = true;

        /// <summary>
        /// Columnas a sincronizar (si es null, sincroniza todas)
        /// </summary>
        public List<string> SyncColumns { get; set; }

        public BulkSyncOptions()
        {
            SyncColumns = new List<string>();
        }
    }

    /// <summary>
    /// Resultado de operaciones en lote
    /// </summary>
    public class BatchOperationResult
    {
        /// <summary>
        /// Indica si todas las operaciones fueron exitosas
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Mensaje de resultado
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Número total de operaciones
        /// </summary>
        public int TotalOperations { get; set; }

        /// <summary>
        /// Número de operaciones exitosas
        /// </summary>
        public int SuccessfulOperations { get; set; }

        /// <summary>
        /// Número de operaciones fallidas
        /// </summary>
        public int FailedOperations { get; set; }

        /// <summary>
        /// Tiempo total de ejecución en milisegundos
        /// </summary>
        public long TotalExecutionTimeMs { get; set; }

        /// <summary>
        /// Detalles de operaciones fallidas
        /// </summary>
        public List<BatchOperationError> Errors { get; set; }

        public BatchOperationResult()
        {
            Errors = new List<BatchOperationError>();
        }
    }

    /// <summary>
    /// Error en operación de lote
    /// </summary>
    public class BatchOperationError
    {
        /// <summary>
        /// Índice de la operación
        /// </summary>
        public int OperationIndex { get; set; }

        /// <summary>
        /// Tipo de operación
        /// </summary>
        public string OperationType { get; set; }

        /// <summary>
        /// Mensaje de error
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Excepción capturada
        /// </summary>
        public Exception Exception { get; set; }
    }

    /// <summary>
    /// Operación individual en un lote
    /// </summary>
    public class BatchOperation
    {
        /// <summary>
        /// Tipo de operación
        /// </summary>
        public BatchOperationType Type { get; set; }

        /// <summary>
        /// Nombre de la tabla
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Datos para la operación
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        /// Opciones específicas para la operación
        /// </summary>
        public object Options { get; set; }

        /// <summary>
        /// Columna clave (para UPDATE/DELETE)
        /// </summary>
        public string KeyColumn { get; set; }
    }

    /// <summary>
    /// Tipos de operaciones en lote
    /// </summary>
    public enum BatchOperationType
    {
        Insert,
        Update,
        Delete,
        Upsert,
        Sync
    }
} 