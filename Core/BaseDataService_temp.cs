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
        /// Ejecuta un procedimiento almacenado y devuelve un resultado tipado
        /// </summary>
        public async Task<StoredProcedureResult<TResult>> EjecutarProcedimientoAsync<TResult>(string connectionString, string procedureName)
        {
            try
            {
                // Ejecutar el procedimiento sin parámetros
                var dataSet = new DataSet();
                using (var connection = new SqlConnection(connectionString))
                using (var command = new SqlCommand(procedureName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    var adapter = new SqlDataAdapter(command);
                    adapter.Fill(dataSet);
                }

                // Mapear el resultado
                var result = new StoredProcedureResult<TResult>(dataSet);
                if (dataSet.Tables.Count > 0)
                {
                    var table = dataSet.Tables[0];
                    if (typeof(TResult).IsGenericType && typeof(TResult).GetGenericTypeDefinition() == typeof(List<>))
                    {
                        var elementType = typeof(TResult).GetGenericArguments()[0];
                        var toListMethod = typeof(BaseDataService).GetMethod(nameof(DataTableToList), BindingFlags.NonPublic | BindingFlags.Static)
                            .MakeGenericMethod(elementType);
                        result.Data = (TResult)toListMethod.Invoke(null, new object[] { table });
                    }
                    else
                    {
                        var toObjectMethod = typeof(BaseDataService).GetMethod(nameof(DataTableToObject), BindingFlags.NonPublic | BindingFlags.Static)
                            .MakeGenericMethod(typeof(TResult));
                        result.Data = (TResult)toObjectMethod.Invoke(null, new object[] { table });
                    }
                }
                result.IsSuccess = true;
                result.Message = "Operación exitosa";
                return result;
            }
            catch (Exception ex)
            {
                return StoredProcedureResult<TResult>.Failure(ex);
            }
        }

        /// <summary>
        /// Ejecuta un procedimiento almacenado con parámetros y devuelve un resultado tipado
