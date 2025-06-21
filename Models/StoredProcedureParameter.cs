using System;
using System.Data;
using LibraryDBApi.Enums;

namespace LibraryDBApi.Models
{
    /// <summary>
    /// Modelo que representa un parámetro de procedimiento almacenado
    /// </summary>
    public class StoredProcedureParameter
    {
        /// <summary>
        /// Nombre del parámetro
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Valor del parámetro
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Tipo de parámetro
        /// </summary>
        public ParameterType ParameterType { get; set; }

        /// <summary>
        /// Tipo de datos SQL Server
        /// </summary>
        public SqlDbType SqlDbType => ParameterType.ToSqlDbType();

        /// <summary>
        /// Tipo de código .NET
        /// </summary>
        public TypeCode TypeCode => ParameterType.ToTypeCode();

        /// <summary>
        /// Dirección del parámetro (Input, Output, InputOutput)
        /// </summary>
        public ParameterDirection Direction { get; set; } = ParameterDirection.Input;

        /// <summary>
        /// Tamaño del parámetro (para tipos de longitud variable)
        /// </summary>
        public int Size { get; set; } = -1;

        /// <summary>
        /// Precisión para tipos decimales
        /// </summary>
        public byte Precision { get; set; } = 18;

        /// <summary>
        /// Escala para tipos decimales
        /// </summary>
        public byte Scale { get; set; } = 2;

        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public StoredProcedureParameter()
        {
            ParameterType = ParameterType.String;
            Direction = ParameterDirection.Input;
        }

        /// <summary>
        /// Constructor con parámetros básicos
        /// </summary>
        /// <param name="name">Nombre del parámetro</param>
        /// <param name="value">Valor del parámetro</param>
        /// <param name="parameterType">Tipo de parámetro</param>
        public StoredProcedureParameter(string name, object value, ParameterType parameterType = ParameterType.String)
        {
            Name = name;
            Value = value;
            ParameterType = parameterType;
            Direction = ParameterDirection.Input;
        }

        /// <summary>
        /// Constructor completo
        /// </summary>
        /// <param name="name">Nombre del parámetro</param>
        /// <param name="value">Valor del parámetro</param>
        /// <param name="parameterType">Tipo de parámetro</param>
        /// <param name="direction">Dirección del parámetro</param>
        /// <param name="size">Tamaño del parámetro</param>
        public StoredProcedureParameter(string name, object value, ParameterType parameterType, ParameterDirection direction, int size = -1)
        {
            Name = name;
            Value = value;
            ParameterType = parameterType;
            Direction = direction;
            Size = size;
        }

        /// <summary>
        /// Convierte el parámetro a SqlParameter
        /// </summary>
        /// <returns>SqlParameter configurado</returns>
        public SqlParameter ToSqlParameter()
        {
            var parameter = new SqlParameter
            {
                ParameterName = Name,
                SqlDbType = SqlDbType,
                Direction = Direction,
                Value = Value ?? DBNull.Value
            };

            if (Size > 0)
                parameter.Size = Size;

            if (ParameterType == ParameterType.Decimal)
            {
                parameter.Precision = Precision;
                parameter.Scale = Scale;
            }

            return parameter;
        }

        /// <summary>
        /// Valida que el parámetro sea válido
        /// </summary>
        /// <returns>True si es válido, False en caso contrario</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Name);
        }

        /// <summary>
        /// Obtiene el valor convertido al tipo correcto
        /// </summary>
        /// <returns>Valor convertido</returns>
        public object GetConvertedValue()
        {
            if (Value == null || Value == DBNull.Value)
                return DBNull.Value;

            try
            {
                return Convert.ChangeType(Value, TypeCode);
            }
            catch
            {
                return Value;
            }
        }
    }
} 