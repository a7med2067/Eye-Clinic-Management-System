using System;
using System.Data.SqlClient;
using System.IO;
using Newtonsoft.Json;

namespace EyeClinic.Data.Configuration
{
    public class ConnectionConfigModel
    {
        public string Server { get; set; } = "localhost";
        public string Database { get; set; } = "EyeClinicDb";
        public bool IntegratedSecurity { get; set; } = true;
        public string SqlUser { get; set; }
        public string SqlPassword { get; set; }
        public int Port { get; set; } = 1433;
        public int ConnectTimeout { get; set; } = 10;
    }

    public static class ConnectionConfig
    {
        private static readonly string ConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "EyeClinic",
            "connection.json"
        );

        private static string _cachedConnectionString;

        public static string GetConnectionString()
        {
            if (!string.IsNullOrEmpty(_cachedConnectionString))
                return _cachedConnectionString;

            var model = LoadConfig();
            _cachedConnectionString = BuildConnectionString(model);
            return _cachedConnectionString;
        }

        public static void SetCustomConnectionString(string connectionString)
        {
            _cachedConnectionString = connectionString;
        }

        public static ConnectionConfigModel LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    var model = JsonConvert.DeserializeObject<ConnectionConfigModel>(json) ?? new ConnectionConfigModel();
                    if (!string.IsNullOrEmpty(model.SqlPassword))
                    {
                        model.SqlPassword = DecryptString(model.SqlPassword);
                    }
                    return model;
                }
            }
            catch
            {
                // Fallback to default local config
            }

            return new ConnectionConfigModel();
        }

        public static void SaveConfig(ConnectionConfigModel model)
        {
            string dir = Path.GetDirectoryName(ConfigFilePath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var toSave = new ConnectionConfigModel
            {
                Server = model.Server,
                Database = model.Database,
                Port = model.Port,
                IntegratedSecurity = model.IntegratedSecurity,
                SqlUser = model.SqlUser,
                SqlPassword = EncryptString(model.SqlPassword),
                ConnectTimeout = model.ConnectTimeout
            };

            string json = JsonConvert.SerializeObject(toSave, Formatting.Indented);
            File.WriteAllText(ConfigFilePath, json);
            _cachedConnectionString = BuildConnectionString(model);
        }

        private static string EncryptString(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;
            try
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(plainText);
                byte[] encrypted = System.Security.Cryptography.ProtectedData.Protect(
                    bytes, null, System.Security.Cryptography.DataProtectionScope.LocalMachine);
                return "ENC:" + Convert.ToBase64String(encrypted);
            }
            catch
            {
                return plainText;
            }
        }

        private static string DecryptString(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText) || !encryptedText.StartsWith("ENC:"))
                return encryptedText;
            try
            {
                string b64 = encryptedText.Substring(4);
                byte[] bytes = Convert.FromBase64String(b64);
                byte[] decrypted = System.Security.Cryptography.ProtectedData.Unprotect(
                    bytes, null, System.Security.Cryptography.DataProtectionScope.LocalMachine);
                return System.Text.Encoding.UTF8.GetString(decrypted);
            }
            catch
            {
                return "";
            }
        }

        public static string BuildConnectionString(ConnectionConfigModel model)
        {
            var builder = new SqlConnectionStringBuilder
            {
                InitialCatalog = string.IsNullOrEmpty(model.Database) ? "EyeClinicDb" : model.Database,
                ConnectTimeout = model.ConnectTimeout > 0 ? model.ConnectTimeout : 10,
                MultipleActiveResultSets = true
            };

            string server = string.IsNullOrEmpty(model.Server) ? "localhost" : model.Server;
            if (model.Port > 0 && model.Port != 1433 && !server.Contains(","))
            {
                server = $"{server},{model.Port}";
            }
            builder.DataSource = server;

            if (model.IntegratedSecurity)
            {
                builder.IntegratedSecurity = true;
            }
            else
            {
                builder.IntegratedSecurity = false;
                builder.UserID = model.SqlUser ?? "";
                builder.Password = model.SqlPassword ?? "";
            }

            return builder.ConnectionString;
        }

        public static (bool success, string message) TestConnection(string connectionString)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT @@VERSION";
                        object result = cmd.ExecuteScalar();
                        return (true, result != null ? result.ToString() : "الاتصال ناجح");
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
