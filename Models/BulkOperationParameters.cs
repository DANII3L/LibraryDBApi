using System.Collections.Generic;

namespace LibraryDBApi.Models
{
    /// <summary>
    /// Parámetros para operaciones masivas usando procedimientos almacenados
    /// </summary>
    public class BulkOperationParameters
    {
        /// <summary>
        /// Cadena de conexión a la base de datos
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Nombre del procedimiento almacenado para la operación masiva
        /// </summary>
        public string ProcedureName { get; set; } = string.Empty;

        /// <summary>
        /// Lista de objetos a procesar en la operación masiva
        /// </summary>
        public IEnumerable<object> Data { get; set; } = new List<object>();

        /// <summary>
        /// Tamaño del lote para procesamiento (opcional, por defecto 1000)
        /// </summary>
        public int BatchSize { get; set; } = 1000;

        /// <summary>
        /// Parámetros adicionales para el procedimiento almacenado (opcional)
        /// </summary>
        public object? AdditionalParameters { get; set; }

        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public BulkOperationParameters()
        {
        }

        /// <summary>
        /// Constructor con parámetros básicos
        /// </summary>
        /// <param name="connectionString">Cadena de conexión</param>
        /// <param name="procedureName">Nombre del procedimiento</param>
        /// <param name="data">Datos a procesar</param>
        /// <param name="batchSize">Tamaño del lote</param>
        public BulkOperationParameters(string connectionString, string procedureName, IEnumerable<object> data, int batchSize = 1000)
        {
            ConnectionString = connectionString;
            ProcedureName = procedureName;
            Data = data;
            BatchSize = batchSize;
        }

        /// <summary>
        /// Constructor completo
        /// </summary>
        /// <param name="connectionString">Cadena de conexión</param>
        /// <param name="procedureName">Nombre del procedimiento</param>
        /// <param name="data">Datos a procesar</param>
        /// <param name="batchSize">Tamaño del lote</param>
        /// <param name="additionalParameters">Parámetros adicionales</param>
        public BulkOperationParameters(string connectionString, string procedureName, IEnumerable<object> data, int batchSize, object? additionalParameters)
        {
            ConnectionString = connectionString;
            ProcedureName = procedureName;
            Data = data;
            BatchSize = batchSize;
            AdditionalParameters = additionalParameters;
        }
    }

    /// <summary>
    /// Parámetros específicos para inserción masiva
    /// </summary>
    public class BulkInsertParameters : BulkOperationParameters
    {
        /// <summary>
        /// Si debe ignorar columnas de identidad (opcional, por defecto true)
        /// </summary>
        public bool IgnoreIdentityColumns { get; set; } = true;

        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public BulkInsertParameters()
        {
        }

        /// <summary>
        /// Constructor con parámetros básicos
        /// </summary>
        /// <param name="connectionString">Cadena de conexión</param>
        /// <param name="procedureName">Nombre del procedimiento</param>
        /// <param name="data">Datos a insertar</param>
        /// <param name="batchSize">Tamaño del lote</param>
        /// <param name="ignoreIdentityColumns">Ignorar columnas de identidad</param>
        public BulkInsertParameters(string connectionString, string procedureName, IEnumerable<object> data, int batchSize = 1000, bool ignoreIdentityColumns = true)
            : base(connectionString, procedureName, data, batchSize)
        {
            IgnoreIdentityColumns = ignoreIdentityColumns;
        }
    }

    /// <summary>
    /// Parámetros específicos para actualización masiva
    /// </summary>
    public class BulkUpdateParameters : BulkOperationParameters
    {
        /// <summary>
        /// Columna clave para identificar los registros a actualizar
        /// </summary>
        public string KeyColumn { get; set; } = string.Empty;

        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public BulkUpdateParameters()
        {
        }

        /// <summary>
        /// Constructor con parámetros básicos
        /// </summary>
        /// <param name="connectionString">Cadena de conexión</param>
        /// <param name="procedureName">Nombre del procedimiento</param>
        /// <param name="data">Datos a actualizar</param>
        /// <param name="keyColumn">Columna clave</param>
        /// <param name="batchSize">Tamaño del lote</param>
        public BulkUpdateParameters(string connectionString, string procedureName, IEnumerable<object> data, string keyColumn, int batchSize = 1000)
            : base(connectionString, procedureName, data, batchSize)
        {
            KeyColumn = keyColumn;
        }
    }
}
