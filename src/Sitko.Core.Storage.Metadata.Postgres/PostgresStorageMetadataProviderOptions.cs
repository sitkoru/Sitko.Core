using Npgsql;

namespace Sitko.Core.Storage.Metadata.Postgres
{
    public class PostgresStorageMetadataProviderOptions : StorageMetadataProviderOptions
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 5432;
        public string Username { get; set; } = "postgres";
        public string Password { get; set; } = string.Empty;
        public string? Database { get; set; }

        public string GetConnectionString()
        {
            var builder = new NpgsqlConnectionStringBuilder();
            if (!string.IsNullOrEmpty(Host)) builder.Host = Host;

            if (Port > 0) builder.Port = Port;

            if (!string.IsNullOrEmpty(Username)) builder.Username = Username;

            if (!string.IsNullOrEmpty(Password)) builder.Password = Password;

            builder.Database = Database;
            // builder.SearchPath = $"{Schema},public";
            return builder.ConnectionString;
        }
    }
}
