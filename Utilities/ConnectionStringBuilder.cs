using System;
using System.Data.SqlClient;
using LibraryDBApi.Models;

namespace LibraryDBApi.Utilities
{
    /// <summary>
    /// Utilidad para construir y validar cadenas de conexión
    /// </summary>
    public static class ConnectionStringBuilder
    {
        /// <summary>
        /// Construye una cadena de conexión básica
        /// </summary>
        /// <param name="server">Servidor</param>
        /// <param name="database">Base de datos</param>
        /// <param name="userId">Usuario</param>
        /// <param name="password">Contraseña</param>
        /// <param name="timeout">Tiempo de espera en segundos</param>
        /// <returns>Cadena de conexión</returns>
        public static string BuildConnectionString(string server, string database, string userId, string password, int timeout = 30)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = server,
                InitialCatalog = database,
                UserID = userId,
                Password = password,
                ConnectionTimeout = timeout,
                CommandTimeout = timeout
            };

            return builder.ConnectionString;
        }

        /// <summary>
        /// Construye una cadena de conexión con autenticación integrada
        /// </summary>
        /// <param name="server">Servidor</param>
        /// <param name="database">Base de datos</param>
        /// <param name="timeout">Tiempo de espera en segundos</param>
        /// <returns>Cadena de conexión</returns>
        public static string BuildConnectionString(string server, string database, int timeout = 30)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = server,
                InitialCatalog = database,
                IntegratedSecurity = true,
                ConnectionTimeout = timeout,
                CommandTimeout = timeout
            };

            return builder.ConnectionString;
        }

        /// <summary>
        /// Construye una cadena de conexión desde un objeto DatabaseConnection
        /// </summary>
        /// <param name="connection">Objeto de conexión</param>
        /// <returns>Cadena de conexión</returns>
        public static string BuildConnectionString(DatabaseConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            return connection.GetConnectionString();
        }

        /// <summary>
        /// Construye una cadena de conexión con opciones avanzadas
        /// </summary>
        /// <param name="server">Servidor</param>
        /// <param name="database">Base de datos</param>
        /// <param name="userId">Usuario</param>
        /// <param name="password">Contraseña</param>
        /// <param name="options">Opciones adicionales</param>
        /// <returns>Cadena de conexión</returns>
        public static string BuildConnectionString(string server, string database, string userId, string password, ConnectionStringOptions options)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = server,
                InitialCatalog = database,
                UserID = userId,
                Password = password
            };

            ApplyOptions(builder, options);

            return builder.ConnectionString;
        }

        /// <summary>
        /// Construye una cadena de conexión con autenticación integrada y opciones avanzadas
        /// </summary>
        /// <param name="server">Servidor</param>
        /// <param name="database">Base de datos</param>
        /// <param name="options">Opciones adicionales</param>
        /// <returns>Cadena de conexión</returns>
        public static string BuildConnectionString(string server, string database, ConnectionStringOptions options)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = server,
                InitialCatalog = database,
                IntegratedSecurity = true
            };

            ApplyOptions(builder, options);

            return builder.ConnectionString;
        }

        /// <summary>
        /// Valida una cadena de conexión
        /// </summary>
        /// <param name="connectionString">Cadena de conexión a validar</param>
        /// <returns>True si es válida, False en caso contrario</returns>
        public static bool ValidateConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return false;

            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                return !string.IsNullOrWhiteSpace(builder.DataSource) && 
                       !string.IsNullOrWhiteSpace(builder.InitialCatalog);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Prueba una cadena de conexión
        /// </summary>
        /// <param name="connectionString">Cadena de conexión a probar</param>
        /// <returns>True si la conexión es exitosa, False en caso contrario</returns>
        public static bool TestConnection(string connectionString)
        {
            if (!ValidateConnectionString(connectionString))
                return false;

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    return connection.State == System.Data.ConnectionState.Open;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Prueba una cadena de conexión de forma asíncrona
        /// </summary>
        /// <param name="connectionString">Cadena de conexión a probar</param>
        /// <returns>Task con el resultado de la prueba</returns>
        public static async System.Threading.Tasks.Task<bool> TestConnectionAsync(string connectionString)
        {
            if (!ValidateConnectionString(connectionString))
                return false;

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    return connection.State == System.Data.ConnectionState.Open;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Obtiene información de una cadena de conexión
        /// </summary>
        /// <param name="connectionString">Cadena de conexión</param>
        /// <returns>Información de la conexión</returns>
        public static ConnectionInfo GetConnectionInfo(string connectionString)
        {
            if (!ValidateConnectionString(connectionString))
                return null;

            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                return new ConnectionInfo
                {
                    Server = builder.DataSource,
                    Database = builder.InitialCatalog,
                    UserId = builder.UserID,
                    IntegratedSecurity = builder.IntegratedSecurity,
                    ConnectionTimeout = builder.ConnectionTimeout,
                    CommandTimeout = builder.CommandTimeout,
                    Encrypt = builder.Encrypt,
                    TrustServerCertificate = builder.TrustServerCertificate
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Aplica opciones a un SqlConnectionStringBuilder
        /// </summary>
        /// <param name="builder">Builder de conexión</param>
        /// <param name="options">Opciones a aplicar</param>
        private static void ApplyOptions(SqlConnectionStringBuilder builder, ConnectionStringOptions options)
        {
            if (options == null)
                return;

            if (options.ConnectionTimeout.HasValue)
                builder.ConnectionTimeout = options.ConnectionTimeout.Value;

            if (options.CommandTimeout.HasValue)
                builder.CommandTimeout = options.CommandTimeout.Value;

            if (options.Encrypt.HasValue)
                builder.Encrypt = options.Encrypt.Value;

            if (options.TrustServerCertificate.HasValue)
                builder.TrustServerCertificate = options.TrustServerCertificate.Value;

            if (options.ApplicationName != null)
                builder.ApplicationName = options.ApplicationName;

            if (options.WorkstationId != null)
                builder.WorkstationID = options.WorkstationId;

            if (options.PacketSize.HasValue)
                builder.PacketSize = options.PacketSize.Value;

            if (options.MultipleActiveResultSets.HasValue)
                builder.MultipleActiveResultSets = options.MultipleActiveResultSets.Value;

            if (options.PersistSecurityInfo.HasValue)
                builder.PersistSecurityInfo = options.PersistSecurityInfo.Value;
        }
    }

    /// <summary>
    /// Opciones para construir cadenas de conexión
    /// </summary>
    public class ConnectionStringOptions
    {
        /// <summary>
        /// Tiempo de espera de conexión en segundos
        /// </summary>
        public int? ConnectionTimeout { get; set; }

        /// <summary>
        /// Tiempo de espera de comando en segundos
        /// </summary>
        public int? CommandTimeout { get; set; }

        /// <summary>
        /// Indica si se debe usar encriptación
        /// </summary>
        public bool? Encrypt { get; set; }

        /// <summary>
        /// Indica si se debe confiar en el certificado del servidor
        /// </summary>
        public bool? TrustServerCertificate { get; set; }

        /// <summary>
        /// Nombre de la aplicación
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// ID de la estación de trabajo
        /// </summary>
        public string WorkstationId { get; set; }

        /// <summary>
        /// Tamaño del paquete en bytes
        /// </summary>
        public int? PacketSize { get; set; }

        /// <summary>
        /// Indica si se permiten múltiples resultados activos
        /// </summary>
        public bool? MultipleActiveResultSets { get; set; }

        /// <summary>
        /// Indica si se debe persistir información de seguridad
        /// </summary>
        public bool? PersistSecurityInfo { get; set; }
    }

    /// <summary>
    /// Información de una cadena de conexión
    /// </summary>
    public class ConnectionInfo
    {
        /// <summary>
        /// Servidor
        /// </summary>
        public string Server { get; set; }

        /// <summary>
        /// Base de datos
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Usuario
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Indica si usa autenticación integrada
        /// </summary>
        public bool IntegratedSecurity { get; set; }

        /// <summary>
        /// Tiempo de espera de conexión
        /// </summary>
        public int ConnectionTimeout { get; set; }

        /// <summary>
        /// Tiempo de espera de comando
        /// </summary>
        public int CommandTimeout { get; set; }

        /// <summary>
        /// Indica si usa encriptación
        /// </summary>
        public bool Encrypt { get; set; }

        /// <summary>
        /// Indica si confía en el certificado del servidor
        /// </summary>
        public bool TrustServerCertificate { get; set; }
    }
} 