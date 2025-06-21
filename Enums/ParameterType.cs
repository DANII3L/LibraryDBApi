using System;
using System.Data;

namespace LibraryDBApi.Enums
{
    /// <summary>
    /// Enum que define los tipos de parámetros soportados para procedimientos almacenados
    /// </summary>
    public enum ParameterType
    {
        /// <summary>
        /// Tipo entero (Int32)
        /// </summary>
        Integer = 0,
        
        /// <summary>
        /// Tipo entero largo (Int64)
        /// </summary>
        Long = 1,
        
        /// <summary>
        /// Tipo decimal
        /// </summary>
        Decimal = 2,
        
        /// <summary>
        /// Tipo string (VARCHAR)
        /// </summary>
        String = 3,
        
        /// <summary>
        /// Tipo fecha y hora
        /// </summary>
        DateTime = 4,
        
        /// <summary>
        /// Tipo booleano
        /// </summary>
        Boolean = 5,
        
        /// <summary>
        /// Tipo GUID
        /// </summary>
        Guid = 6,
        
        /// <summary>
        /// Tipo binario
        /// </summary>
        Binary = 7,
        
        /// <summary>
        /// Tipo XML
        /// </summary>
        Xml = 8,
        
        /// <summary>
        /// Tipo tabla (Table-Valued Parameter)
        /// </summary>
        Table = 9
    }

    /// <summary>
    /// Extensiones para el enum ParameterType
    /// </summary>
    public static class ParameterTypeExtensions
    {
        /// <summary>
        /// Convierte un ParameterType a SqlDbType
        /// </summary>
        /// <param name="parameterType">Tipo de parámetro</param>
        /// <returns>SqlDbType correspondiente</returns>
        public static SqlDbType ToSqlDbType(this ParameterType parameterType)
        {
            return parameterType switch
            {
                ParameterType.Integer => SqlDbType.Int,
                ParameterType.Long => SqlDbType.BigInt,
                ParameterType.Decimal => SqlDbType.Decimal,
                ParameterType.String => SqlDbType.VarChar,
                ParameterType.DateTime => SqlDbType.DateTime,
                ParameterType.Boolean => SqlDbType.Bit,
                ParameterType.Guid => SqlDbType.UniqueIdentifier,
                ParameterType.Binary => SqlDbType.VarBinary,
                ParameterType.Xml => SqlDbType.Xml,
                ParameterType.Table => SqlDbType.Structured,
                _ => SqlDbType.VarChar
            };
        }

        /// <summary>
        /// Convierte un ParameterType a TypeCode
        /// </summary>
        /// <param name="parameterType">Tipo de parámetro</param>
        /// <returns>TypeCode correspondiente</returns>
        public static TypeCode ToTypeCode(this ParameterType parameterType)
        {
            return parameterType switch
            {
                ParameterType.Integer => TypeCode.Int32,
                ParameterType.Long => TypeCode.Int64,
                ParameterType.Decimal => TypeCode.Decimal,
                ParameterType.String => TypeCode.String,
                ParameterType.DateTime => TypeCode.DateTime,
                ParameterType.Boolean => TypeCode.Boolean,
                ParameterType.Guid => TypeCode.String,
                ParameterType.Binary => TypeCode.Object,
                ParameterType.Xml => TypeCode.String,
                ParameterType.Table => TypeCode.Object,
                _ => TypeCode.String
            };
        }
    }
} 