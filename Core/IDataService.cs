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
        /// Ejecuta un procedimiento almacenado y devuelve un resultado tipado como IEnumerable usando parámetros unificados
        /// </summary>
        Task<StoredProcedureResult<IEnumerable<TResult>>> EjecutarProcedimientoAsync<TResult>(StoredProcedureParameters parameters) where TResult : new();

        /// <summary>
        /// Ejecuta un procedimiento almacenado sin modelo y devuelve un resultado tipado como IEnumerable
        /// </summary>
        Task<StoredProcedureResult<IEnumerable<TResult>>> EjecutarProcedimientoAsync<TResult>(string connectionString, string procedureName, ModelPaginacion modelPaginacion = null) where TResult : new();
    }
} 