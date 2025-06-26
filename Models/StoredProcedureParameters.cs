using System;

namespace LibraryDBApi.Models
{
    /// <summary>
    /// Modelo unificado para los parámetros de ejecución de procedimientos almacenados
    /// </summary>
    public class StoredProcedureParameters
    {
        /// <summary>
        /// Cadena de conexión a la base de datos
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Nombre del procedimiento almacenado a ejecutar
        /// </summary>
        public string ProcedureName { get; set; }

        /// <summary>
        /// Modelo con los parámetros del procedimiento almacenado (opcional)
        /// </summary>
        public object Model { get; set; }

        /// <summary>
        /// Modelo de paginación que incluye PageSize, PageNumber y Filter (opcional)
        /// </summary>
        public ModelPaginacion? ModelPaginacion { get; set; } = null;

        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public StoredProcedureParameters() { }

        /// <summary>
        /// Constructor con parámetros básicos
        /// </summary>
        /// <param name="connectionString">Cadena de conexión</param>
        /// <param name="procedureName">Nombre del procedimiento</param>
        public StoredProcedureParameters(string connectionString, string procedureName)
        {
            ConnectionString = connectionString;
            ProcedureName = procedureName;
        }

        /// <summary>
        /// Constructor completo con todos los parámetros
        /// </summary>
        /// <param name="connectionString">Cadena de conexión</param>
        /// <param name="procedureName">Nombre del procedimiento</param>
        /// <param name="model">Modelo con parámetros</param>
        /// <param name="modelPaginacion">Modelo de paginación</param>
        public StoredProcedureParameters(string connectionString, string procedureName, object model = null, ModelPaginacion modelPaginacion = null)
        {
            ConnectionString = connectionString;
            ProcedureName = procedureName;
            Model = model;
            ModelPaginacion = modelPaginacion;
        }
    }
} 