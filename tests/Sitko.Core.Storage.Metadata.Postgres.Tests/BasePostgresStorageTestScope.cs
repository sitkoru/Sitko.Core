using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;
using Sitko.Core.Storage.S3;
using Sitko.Core.Xunit;

namespace Sitko.Core.Storage.Metadata.Postgres.Tests
{
    public class BasePostgresStorageTestScope : BaseTestScope
    {
        private Guid _bucketName = Guid.NewGuid();

        protected override TestApplication ConfigureApplication(TestApplication application, string name)
        {
            return base.ConfigureApplication(application, name)
                .AddModule<S3StorageModule<TestS3StorageSettings>, TestS3StorageSettings>(
                    (configuration, _, moduleConfig) =>
                    {
                        moduleConfig.PublicUri = new Uri(configuration["MINIO_SERVER_URI"] + "/" + _bucketName);
                        moduleConfig.Server = new Uri(configuration["MINIO_SERVER_URI"]);
                        moduleConfig.Bucket = _bucketName.ToString();
                        moduleConfig.Prefix = "test";
                        moduleConfig.AccessKey = configuration["MINIO_ACCESS_KEY"];
                        moduleConfig.SecretKey = configuration["MINIO_SECRET_KEY"];
                        moduleConfig
                            .EnableMetadata<PostgresStorageMetadataProvider<TestS3StorageSettings>,
                                PostgresStorageMetadataProviderOptions>(options =>
                            {
                                var builder = new NpgsqlConnectionStringBuilder();
                                if (!string.IsNullOrEmpty(configuration["POSTGRES_HOST"]))
                                {
                                    builder.Host = configuration["POSTGRES_HOST"];
                                }

                                if (int.TryParse(configuration["POSTGRES_PORT"], out var parsedPort))
                                {
                                    builder.Port = parsedPort;
                                }

                                if (!string.IsNullOrEmpty(configuration["POSTGRES_USERNAME"]))
                                {
                                    builder.Username = configuration["POSTGRES_USERNAME"];
                                }

                                if (!string.IsNullOrEmpty(configuration["POSTGRES_PASSWORD"]))
                                {
                                    builder.Password = configuration["POSTGRES_PASSWORD"];
                                }

                                builder.Database = _bucketName.ToString();
                                options.ConnectionString = builder.ConnectionString;
                            });
                    });
        }

        public override async ValueTask DisposeAsync()
        {
            var storage = Get<IStorage<TestS3StorageSettings>>();
            await storage.DeleteAllAsync();
            await base.DisposeAsync();
        }
    }
}
