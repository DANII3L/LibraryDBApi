namespace LibraryDBApi.Models
{
    /// <summary>
    /// Modelo para parámetros de paginación y filtrado
    /// </summary>
    public class ModelPaginacion
    {
        /// <summary>
        /// Número de página para paginación (opcional)
        /// </summary>
        public int? PageNumber { get; set; }

        /// <summary>
        /// Tamaño de página para paginación (opcional)
        /// </summary>
        public int? PageSize { get; set; }

        /// <summary>
        /// Filtro de búsqueda como string (opcional)
        /// </summary>
        public string Filter { get; set; }

        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public ModelPaginacion() { }

        /// <summary>
        /// Constructor con parámetros de paginación
        /// </summary>
        /// <param name="pageNumber">Número de página</param>
        /// <param name="pageSize">Tamaño de página</param>
        /// <param name="filter">Filtro de búsqueda</param>
        public ModelPaginacion(int? pageNumber = null, int? pageSize = null, string filter = null)
        {
            PageNumber = pageNumber;
            PageSize = pageSize;
            Filter = filter;
        }
    }
} 