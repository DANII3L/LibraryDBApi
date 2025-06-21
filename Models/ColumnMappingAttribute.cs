using System;

namespace LibraryDBApi.Models
{
    /// <summary>
    /// Atributo para mapear explícitamente una propiedad a una columna específica
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ColumnMappingAttribute : Attribute
    {
        /// <summary>
        /// Nombre de la columna en la base de datos
        /// </summary>
        public string ColumnName { get; }

        /// <summary>
        /// Indica si la columna es opcional (puede no existir en el resultado)
        /// </summary>
        public bool IsOptional { get; set; }

        /// <summary>
        /// Valor por defecto si la columna no existe o es null
        /// </summary>
        public object DefaultValue { get; set; }

        /// <summary>
        /// Constructor del atributo
        /// </summary>
        /// <param name="columnName">Nombre de la columna en la base de datos</param>
        public ColumnMappingAttribute(string columnName)
        {
            ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
        }

        /// <summary>
        /// Constructor del atributo con opciones adicionales
        /// </summary>
        /// <param name="columnName">Nombre de la columna en la base de datos</param>
        /// <param name="isOptional">Indica si la columna es opcional</param>
        /// <param name="defaultValue">Valor por defecto</param>
        public ColumnMappingAttribute(string columnName, bool isOptional = false, object defaultValue = null)
        {
            ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
            IsOptional = isOptional;
            DefaultValue = defaultValue;
        }
    }

    /// <summary>
    /// Atributo para ignorar una propiedad en el mapeo
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class IgnoreMappingAttribute : Attribute
    {
        /// <summary>
        /// Razón por la que se ignora el mapeo
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// Constructor del atributo
        /// </summary>
        /// <param name="reason">Razón opcional por la que se ignora</param>
        public IgnoreMappingAttribute(string reason = null)
        {
            Reason = reason;
        }
    }

    /// <summary>
    /// Atributo para especificar el tipo de conversión personalizada
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class CustomConversionAttribute : Attribute
    {
        /// <summary>
        /// Tipo de conversión personalizada
        /// </summary>
        public Type ConversionType { get; }

        /// <summary>
        /// Formato para conversión (opcional)
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// Constructor del atributo
        /// </summary>
        /// <param name="conversionType">Tipo de conversión personalizada</param>
        public CustomConversionAttribute(Type conversionType)
        {
            ConversionType = conversionType ?? throw new ArgumentNullException(nameof(conversionType));
        }

        /// <summary>
        /// Constructor del atributo con formato
        /// </summary>
        /// <param name="conversionType">Tipo de conversión personalizada</param>
        /// <param name="format">Formato para conversión</param>
        public CustomConversionAttribute(Type conversionType, string format)
        {
            ConversionType = conversionType ?? throw new ArgumentNullException(nameof(conversionType));
            Format = format;
        }
    }
} 