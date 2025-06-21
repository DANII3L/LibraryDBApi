using System;
using System.Data.SqlClient;
using System.Data;

namespace LibraryDBApi.Models
{
    /// <summary>
    /// Modelo que representa la información de conexión a una base de datos
    /// </summary>
    public class DatabaseConnection
    {
        /// <summary>
        /// Servidor de base de datos
        /// </summary>
        public string Server { get; set; }

        /// <summary>
        /// Nombre de la base de datos
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Usuario de la base de datos
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Contraseña del usuario
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Tiempo de espera de conexión en segundos
        /// </summary>
        public int ConnectionTimeout { get; set; } = 30;

        /// <summary>
        /// Tiempo de espera de comando en segundos
        /// </summary>
        public int CommandTimeout { get; set; } = 30;

        /// <summary>
        /// Indica si se debe usar autenticación integrada de Windows
        /// </summary>
        public bool IntegratedSecurity { get; set; } = false;

        /// <summary>
        /// Indica si se debe usar encriptación
        /// </summary>
        public bool Encrypt { get; set; } = false;

        /// <summary>
        /// Indica si se debe confiar en el certificado del servidor
        /// </summary>
        public bool TrustServerCertificate { get; set; } = false;

        /// <summary>
        /// Versión de SQL Server
        /// </summary>
        public string ServerVersion { get; set; }

        /// <summary>
        /// Estado de la conexión
        /// </summary>
        public ConnectionState State { get; set; }

        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public DatabaseConnection()
        {
        }

        /// <summary>
        /// Constructor con parámetros básicos
        /// </summary>
        /// <param name="server">Servidor</param>
        /// <param name="database">Base de datos</param>
        /// <param name="userId">Usuario</param>
        /// <param name="password">Contraseña</param>
        public DatabaseConnection(string server, string database, string userId, string password)
        {
            Server = server;
            Database = database;
            UserId = userId;
            Password = password;
            IntegratedSecurity = false;
        }

        /// <summary>
        /// Constructor con autenticación integrada
        /// </summary>
        /// <param name="server">Servidor</param>
        /// <param name="database">Base de datos</param>
        public DatabaseConnection(string server, string database)
        {
            Server = server;
            Database = database;
            IntegratedSecurity = true;
        }

        /// <summary>
        /// Constructor desde cadena de conexión
        /// </summary>
        /// <param name="connectionString">Cadena de conexión</param>
        public DatabaseConnection(string connectionString)
        {
            ParseConnectionString(connectionString);
        }

        /// <summary>
        /// Genera la cadena de conexión
        /// </summary>
        /// <returns>Cadena de conexión formateada</returns>
        public string GetConnectionString()
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = Server,
                InitialCatalog = Database,
                Encrypt = Encrypt,
                TrustServerCertificate = TrustServerCertificate
            };

            // Agregar timeouts como parámetros de cadena de conexión
            builder["Connection Timeout"] = ConnectionTimeout;
            builder["Command Timeout"] = CommandTimeout;

            if (IntegratedSecurity)
            {
                builder.IntegratedSecurity = true;
            }
            else
            {
                builder.UserID = UserId;
                builder.Password = Password;
            }

            return builder.ConnectionString;
        }

        /// <summary>
        /// Parsea una cadena de conexión y extrae la información
        /// </summary>
        /// <param name="connectionString">Cadena de conexión a parsear</param>
        public void ParseConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return;

            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                
                Server = builder.DataSource;
                Database = builder.InitialCatalog;
                UserId = builder.UserID;
                Password = builder.Password;
                ConnectionTimeout = builder.ContainsKey("Connection Timeout") ? (int)builder["Connection Timeout"] : 30;
                CommandTimeout = builder.ContainsKey("Command Timeout") ? (int)builder["Command Timeout"] : 30;
                IntegratedSecurity = builder.IntegratedSecurity;
                Encrypt = builder.Encrypt;
                TrustServerCertificate = builder.TrustServerCertificate;
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Error al parsear la cadena de conexión: {ex.Message}", nameof(connectionString));
            }
        }

        /// <summary>
        /// Valida que la información de conexión sea válida
        /// </summary>
        /// <returns>True si es válida, False en caso contrario</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Server) && 
                   !string.IsNullOrWhiteSpace(Database) &&
                   (IntegratedSecurity || (!string.IsNullOrWhiteSpace(UserId) && !string.IsNullOrWhiteSpace(Password)));
        }

        /// <summary>
        /// Prueba la conexión a la base de datos
        /// </summary>
        /// <returns>True si la conexión es exitosa, False en caso contrario</returns>
        public bool TestConnection()
        {
            try
            {
                using (var connection = new SqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    State = connection.State;
                    ServerVersion = connection.ServerVersion;
                    return true;
                }
            }
            catch
            {
                State = ConnectionState.Broken;
                return false;
            }
        }

        /// <summary>
        /// Prueba la conexión de forma asíncrona
        /// </summary>
        /// <returns>Task con el resultado de la prueba</returns>
        public async System.Threading.Tasks.Task<bool> TestConnectionAsync()
        {
            try
            {
                using (var connection = new SqlConnection(GetConnectionString()))
                {
                    await connection.OpenAsync();
                    State = connection.State;
                    ServerVersion = connection.ServerVersion;
                    return true;
                }
            }
            catch
            {
                State = ConnectionState.Broken;
                return false;
            }
        }

        /// <summary>
        /// Obtiene información del servidor
        /// </summary>
        /// <returns>Información del servidor o null si no se puede conectar</returns>
        public ServerInfo GetServerInfo()
        {
            try
            {
                using (var connection = new SqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    return new ServerInfo
                    {
                        ServerName = connection.DataSource,
                        DatabaseName = connection.Database,
                        ServerVersion = connection.ServerVersion,
                        ConnectionTimeout = connection.ConnectionTimeout,
                        State = connection.State
                    };
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Clona la información de conexión
        /// </summary>
        /// <returns>Nueva instancia con la misma información</returns>
        public DatabaseConnection Clone()
        {
            return new DatabaseConnection
            {
                Server = Server,
                Database = Database,
                UserId = UserId,
                Password = Password,
                ConnectionTimeout = ConnectionTimeout,
                CommandTimeout = CommandTimeout,
                IntegratedSecurity = IntegratedSecurity,
                Encrypt = Encrypt,
                TrustServerCertificate = TrustServerCertificate,
                ServerVersion = ServerVersion,
                State = State
            };
        }
    }

    /// <summary>
    /// Información del servidor de base de datos
    /// </summary>
    public class ServerInfo
    {
        /// <summary>
        /// Nombre del servidor
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Nombre de la base de datos
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Versión del servidor
        /// </summary>
        public string ServerVersion { get; set; }

        /// <summary>
        /// Tiempo de espera de conexión
        /// </summary>
        public int ConnectionTimeout { get; set; }

        /// <summary>
        /// Estado de la conexión
        /// </summary>
        public ConnectionState State { get; set; }
    }
} 