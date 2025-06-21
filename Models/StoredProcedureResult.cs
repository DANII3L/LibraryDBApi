using System;
using System.Collections.Generic;
using System.Data;

namespace LibraryDBApi.Models
{
    /// <summary>
    /// Modelo que representa el resultado de un procedimiento almacenado
    /// </summary>
    /// <typeparam name="T">Tipo de datos del resultado</typeparam>
    public class StoredProcedureResult<T>
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
        /// Datos del resultado
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// DataSet completo del resultado
        /// </summary>
        public DataSet DataSet { get; set; }

        /// <summary>
        /// Lista de tablas de datos
        /// </summary>
        public List<DataTable> Tables { get; set; }

        /// <summary>
        /// Código de error (si aplica)
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// Excepción capturada (si aplica)
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Tiempo de ejecución en milisegundos
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Total de registros disponibles (para paginación)
        /// </summary>
        public int? TotalRecords { get; set; }

        /// <summary>
        /// Número de página actual (para paginación)
        /// </summary>
        public int? PageNumber { get; set; }

        /// <summary>
        /// Tamaño de la página (cantidad de registros por página para paginación)
        /// </summary>
        public int? PageSize { get; set; }

        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public StoredProcedureResult()
        {
            Tables = new List<DataTable>();
            IsSuccess = false;
            Message = string.Empty;
        }

        /// <summary>
        /// Constructor con parámetros básicos
        /// </summary>
        /// <param name="isSuccess">Indica si fue exitoso</param>
        /// <param name="message">Mensaje de resultado</param>
        /// <param name="data">Datos del resultado</param>
        public StoredProcedureResult(bool isSuccess, string message, T data = default(T))
        {
            IsSuccess = isSuccess;
            Message = message;
            Data = data;
            Tables = new List<DataTable>();
        }

        /// <summary>
        /// Constructor con DataSet
        /// </summary>
        /// <param name="dataSet">DataSet del resultado</param>
        /// <param name="isSuccess">Indica si fue exitoso</param>
        /// <param name="message">Mensaje de resultado</param>
        public StoredProcedureResult(DataSet dataSet, bool isSuccess = true, string message = "Operación exitosa")
        {
            DataSet = dataSet;
            IsSuccess = isSuccess;
            Message = message;
            Tables = new List<DataTable>();

            if (dataSet?.Tables != null)
            {
                foreach (DataTable table in dataSet.Tables)
                {
                    Tables.Add(table);
                }
            }
        }

        /// <summary>
        /// Constructor con excepción
        /// </summary>
        /// <param name="exception">Excepción capturada</param>
        /// <param name="message">Mensaje personalizado</param>
        public StoredProcedureResult(Exception exception, string message = null)
        {
            IsSuccess = false;
            Exception = exception;
            Message = message ?? exception?.Message ?? "Error desconocido";
            Tables = new List<DataTable>();
        }

        /// <summary>
        /// Crea un resultado exitoso
        /// </summary>
        /// <param name="data">Datos del resultado</param>
        /// <param name="message">Mensaje de resultado</param>
        /// <returns>Resultado exitoso</returns>
        public static StoredProcedureResult<T> Success(T data, string message = "Operación exitosa")
        {
            return new StoredProcedureResult<T>(true, message, data);
        }

        /// <summary>
        /// Crea un resultado exitoso con DataSet
        /// </summary>
        /// <param name="dataSet">DataSet del resultado</param>
        /// <param name="message">Mensaje de resultado</param>
        /// <returns>Resultado exitoso</returns>
        public static StoredProcedureResult<T> Success(DataSet dataSet, string message = "Operación exitosa")
        {
            return new StoredProcedureResult<T>(dataSet, true, message);
        }

        /// <summary>
        /// Crea un resultado fallido
        /// </summary>
        /// <param name="message">Mensaje de error</param>
        /// <param name="errorCode">Código de error</param>
        /// <returns>Resultado fallido</returns>
        public static StoredProcedureResult<T> Failure(string message, int errorCode = 0)
        {
            return new StoredProcedureResult<T>(false, message)
            {
                ErrorCode = errorCode
            };
        }

        /// <summary>
        /// Crea un resultado fallido con excepción
        /// </summary>
        /// <param name="exception">Excepción capturada</param>
        /// <param name="message">Mensaje personalizado</param>
        /// <returns>Resultado fallido</returns>
        public static StoredProcedureResult<T> Failure(Exception exception, string message = null)
        {
            return new StoredProcedureResult<T>(exception, message);
        }

        /// <summary>
        /// Obtiene la primera tabla de datos
        /// </summary>
        /// <returns>Primera tabla o null si no existe</returns>
        public DataTable GetFirstTable()
        {
            return Tables.Count > 0 ? Tables[0] : null;
        }

        /// <summary>
        /// Obtiene una tabla específica por índice
        /// </summary>
        /// <param name="index">Índice de la tabla</param>
        /// <returns>Tabla en el índice especificado o null si no existe</returns>
        public DataTable GetTable(int index)
        {
            return index >= 0 && index < Tables.Count ? Tables[index] : null;
        }

        /// <summary>
        /// Obtiene una tabla específica por nombre
        /// </summary>
        /// <param name="tableName">Nombre de la tabla</param>
        /// <returns>Tabla con el nombre especificado o null si no existe</returns>
        public DataTable GetTable(string tableName)
        {
            return Tables.Find(t => string.Equals(t.TableName, tableName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Convierte el resultado a otro tipo
        /// </summary>
        /// <typeparam name="TNew">Nuevo tipo de resultado</typeparam>
        /// <param name="converter">Función de conversión</param>
        /// <returns>Nuevo resultado convertido</returns>
        public StoredProcedureResult<TNew> ConvertTo<TNew>(Func<T, TNew> converter)
        {
            var newResult = new StoredProcedureResult<TNew>
            {
                IsSuccess = IsSuccess,
                Message = Message,
                ErrorCode = ErrorCode,
                Exception = Exception,
                ExecutionTimeMs = ExecutionTimeMs,
                DataSet = DataSet,
                Tables = Tables
            };

            if (IsSuccess && Data != null && converter != null)
            {
                try
                {
                    newResult.Data = converter(Data);
                }
                catch (Exception ex)
                {
                    newResult.IsSuccess = false;
                    newResult.Message = $"Error en conversión: {ex.Message}";
                    newResult.Exception = ex;
                }
            }

            return newResult;
        }
    }
} 