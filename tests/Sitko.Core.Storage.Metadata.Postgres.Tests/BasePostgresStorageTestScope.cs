using System.Threading.Tasks;
using Sitko.Core.Storage.S3;
using Sitko.Core.Xunit;

namespace Sitko.Core.Storage.Metadata.Postgres.Tests
{
    public class BasePostgresStorageTestScope : BaseTestScope
    {
        protected override TestApplication ConfigureApplication(TestApplication application, string name)
        {
            base.ConfigureApplication(application, name);
            application.AddS3Storage<TestS3StorageSettings>(moduleOptions =>
            {
                moduleOptions.Bucket = name.ToLowerInvariant();
                moduleOptions.Prefix = "test";
            });
            application.AddPostgresStorageMetadata<TestS3StorageSettings>(
                moduleOptions =>
                {
                    moduleOptions.Database = name;
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
