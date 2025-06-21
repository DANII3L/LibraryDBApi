using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LibraryDBApi.Enums;
using LibraryDBApi.Models;

namespace LibraryDBApi.Utilities
{
    /// <summary>
    /// Utilidad para validar parámetros de procedimientos almacenados
    /// </summary>
    public static class ParameterValidator
    {
        /// <summary>
        /// Valida una lista de parámetros
        /// </summary>
        /// <param name="parameters">Lista de parámetros a validar</param>
        /// <returns>Resultado de la validación</returns>
        public static ValidationResult ValidateParameters(List<StoredProcedureParameter> parameters)
        {
            var result = new ValidationResult();

            if (parameters == null)
            {
                result.IsValid = false;
                result.Errors.Add("La lista de parámetros no puede ser null");
                return result;
            }

            foreach (var parameter in parameters)
            {
                var parameterValidation = ValidateParameter(parameter);
                if (!parameterValidation.IsValid)
                {
                    result.IsValid = false;
                    result.Errors.AddRange(parameterValidation.Errors);
                }
            }

            return result;
        }

        /// <summary>
        /// Valida un parámetro individual
        /// </summary>
        /// <param name="parameter">Parámetro a validar</param>
        /// <returns>Resultado de la validación</returns>
        public static ValidationResult ValidateParameter(StoredProcedureParameter parameter)
        {
            var result = new ValidationResult();

            if (parameter == null)
            {
                result.IsValid = false;
                result.Errors.Add("El parámetro no puede ser null");
                return result;
            }

            // Validar nombre del parámetro
            if (string.IsNullOrWhiteSpace(parameter.Name))
            {
                result.IsValid = false;
                result.Errors.Add("El nombre del parámetro no puede estar vacío");
            }
            else if (!IsValidParameterName(parameter.Name))
            {
                result.IsValid = false;
                result.Errors.Add($"El nombre del parámetro '{parameter.Name}' no es válido");
            }

            // Validar valor según el tipo
            if (parameter.Value != null && parameter.Value != DBNull.Value)
            {
                var valueValidation = ValidateParameterValue(parameter);
                if (!valueValidation.IsValid)
                {
                    result.IsValid = false;
                    result.Errors.AddRange(valueValidation.Errors);
                }
            }

            return result;
        }

        /// <summary>
        /// Valida el nombre de un parámetro
        /// </summary>
        /// <param name="parameterName">Nombre del parámetro</param>
        /// <returns>True si es válido, False en caso contrario</returns>
        public static bool IsValidParameterName(string parameterName)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
                return false;

            // Los nombres de parámetros deben seguir las reglas de SQL Server
            var pattern = @"^[a-zA-Z_@][a-zA-Z0-9_@]*$";
            return Regex.IsMatch(parameterName, pattern);
        }

        /// <summary>
        /// Valida el valor de un parámetro según su tipo
        /// </summary>
        /// <param name="parameter">Parámetro a validar</param>
        /// <returns>Resultado de la validación</returns>
        public static ValidationResult ValidateParameterValue(StoredProcedureParameter parameter)
        {
            var result = new ValidationResult();

            if (parameter.Value == null || parameter.Value == DBNull.Value)
                return result;

            try
            {
                switch (parameter.ParameterType)
                {
                    case ParameterType.Integer:
                        ValidateIntegerValue(parameter.Value, result);
                        break;

                    case ParameterType.Long:
                        ValidateLongValue(parameter.Value, result);
                        break;

                    case ParameterType.Decimal:
                        ValidateDecimalValue(parameter.Value, result);
                        break;

                    case ParameterType.String:
                        ValidateStringValue(parameter.Value, result);
                        break;

                    case ParameterType.DateTime:
                        ValidateDateTimeValue(parameter.Value, result);
                        break;

                    case ParameterType.Boolean:
                        ValidateBooleanValue(parameter.Value, result);
                        break;

                    case ParameterType.Guid:
                        ValidateGuidValue(parameter.Value, result);
                        break;

                    default:
                        result.IsValid = false;
                        result.Errors.Add($"Tipo de parámetro '{parameter.ParameterType}' no soportado");
                        break;
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"Error al validar el valor: {ex.Message}");
            }

            return result;
        }

        #region Validaciones específicas por tipo

        private static void ValidateIntegerValue(object value, ValidationResult result)
        {
            if (!int.TryParse(value.ToString(), out _))
            {
                result.IsValid = false;
                result.Errors.Add("El valor debe ser un entero válido");
            }
        }

        private static void ValidateLongValue(object value, ValidationResult result)
        {
            if (!long.TryParse(value.ToString(), out _))
            {
                result.IsValid = false;
                result.Errors.Add("El valor debe ser un entero largo válido");
            }
        }

        private static void ValidateDecimalValue(object value, ValidationResult result)
        {
            if (!decimal.TryParse(value.ToString(), out _))
            {
                result.IsValid = false;
                result.Errors.Add("El valor debe ser un decimal válido");
            }
        }

        private static void ValidateStringValue(object value, ValidationResult result)
        {
            var stringValue = value.ToString();
            if (stringValue.Length > 8000)
            {
                result.IsValid = false;
                result.Errors.Add("La longitud del string no puede exceder 8000 caracteres");
            }
        }

        private static void ValidateDateTimeValue(object value, ValidationResult result)
        {
            if (!DateTime.TryParse(value.ToString(), out _))
            {
                result.IsValid = false;
                result.Errors.Add("El valor debe ser una fecha válida");
            }
        }

        private static void ValidateBooleanValue(object value, ValidationResult result)
        {
            if (!bool.TryParse(value.ToString(), out _))
            {
                result.IsValid = false;
                result.Errors.Add("El valor debe ser un booleano válido");
            }
        }

        private static void ValidateGuidValue(object value, ValidationResult result)
        {
            if (!Guid.TryParse(value.ToString(), out _))
            {
                result.IsValid = false;
                result.Errors.Add("El valor debe ser un GUID válido");
            }
        }

        #endregion

        /// <summary>
        /// Valida que los parámetros requeridos estén presentes
        /// </summary>
        /// <param name="parameters">Lista de parámetros</param>
        /// <param name="requiredParameterNames">Nombres de parámetros requeridos</param>
        /// <returns>Resultado de la validación</returns>
        public static ValidationResult ValidateRequiredParameters(List<StoredProcedureParameter> parameters, params string[] requiredParameterNames)
        {
            var result = new ValidationResult();

            if (parameters == null)
            {
                result.IsValid = false;
                result.Errors.Add("La lista de parámetros no puede ser null");
                return result;
            }

            foreach (var requiredName in requiredParameterNames)
            {
                if (!parameters.Any(p => string.Equals(p.Name, requiredName, StringComparison.OrdinalIgnoreCase)))
                {
                    result.IsValid = false;
                    result.Errors.Add($"El parámetro requerido '{requiredName}' no está presente");
                }
            }

            return result;
        }

        /// <summary>
        /// Valida que no haya parámetros duplicados
        /// </summary>
        /// <param name="parameters">Lista de parámetros</param>
        /// <returns>Resultado de la validación</returns>
        public static ValidationResult ValidateNoDuplicateParameters(List<StoredProcedureParameter> parameters)
        {
            var result = new ValidationResult();

            if (parameters == null)
                return result;

            var duplicateNames = parameters
                .GroupBy(p => p.Name.ToLower())
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateNames.Any())
            {
                result.IsValid = false;
                result.Errors.Add($"Parámetros duplicados encontrados: {string.Join(", ", duplicateNames)}");
            }

            return result;
        }
    }

    /// <summary>
    /// Resultado de una validación
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Indica si la validación fue exitosa
        /// </summary>
        public bool IsValid { get; set; } = true;

        /// <summary>
        /// Lista de errores encontrados
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Obtiene el mensaje de error combinado
        /// </summary>
        /// <returns>Mensaje de error</returns>
        public string GetErrorMessage()
        {
            return string.Join("; ", Errors);
        }
    }
} 