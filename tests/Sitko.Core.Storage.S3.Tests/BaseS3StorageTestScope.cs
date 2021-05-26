using System;
using System.Threading.Tasks;
using Sitko.Core.App;
using Sitko.Core.Xunit;

namespace Sitko.Core.Storage.S3.Tests
{
    public class BaseS3StorageTestScope : BaseTestScope
    {
        private Guid _bucketName = Guid.NewGuid();

        protected override TestApplication ConfigureApplication(TestApplication application, string name)
        {
            return base.ConfigureApplication(application, name)
                .AddModule<TestApplication, S3StorageModule<TestS3StorageSettings>, TestS3StorageSettings>(
                    (_, _, moduleConfig) =>
                    {
                        moduleConfig.Bucket = _bucketName.ToString();
                        moduleConfig.Prefix = "test";
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
