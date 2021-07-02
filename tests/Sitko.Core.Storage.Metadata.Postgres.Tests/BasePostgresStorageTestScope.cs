using System.Threading.Tasks;
using Sitko.Core.App;
using Sitko.Core.Storage.S3;
using Sitko.Core.Xunit;

namespace Sitko.Core.Storage.Metadata.Postgres.Tests
{
    public class BasePostgresStorageTestScope : BaseTestScope
    {
        protected override TestApplication ConfigureApplication(TestApplication application, string name)
        {
            base.ConfigureApplication(application, name)
                .AddModule<TestApplication, S3StorageModule<TestS3StorageSettings>, TestS3StorageSettings>(
                    (_, _, moduleConfig) =>
                    {
                        moduleConfig.Bucket = name.ToLowerInvariant();
                        moduleConfig.Prefix = "test";
                    })
                .AddModule<PostgresStorageMetadataModule<TestS3StorageSettings>,
                    PostgresStorageMetadataProviderOptions>((_, _, moduleConfig) =>
                {
                    moduleConfig.Database = name;
                });
            return application;
        }

        public override async ValueTask DisposeAsync()
        {
            var storage = Get<IStorage<TestS3StorageSettings>>();
            await storage.DeleteAllAsync();
            await base.DisposeAsync();
        }
    }
}
