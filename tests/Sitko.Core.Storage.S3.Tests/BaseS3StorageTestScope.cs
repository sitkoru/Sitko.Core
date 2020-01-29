using System;
using System.Threading.Tasks;
using Sitko.Core.Xunit;

namespace Sitko.Core.Storage.S3.Tests
{
    public class BaseS3StorageTestScope : BaseTestScope
    {
        private Guid _bucketName = Guid.NewGuid();

        protected override TestApplication ConfigureApplication(TestApplication application, string name)
        {
            return base.ConfigureApplication(application, name)
                .AddModule<S3StorageModule<TestS3StorageSettings>, TestS3StorageSettings>(
                    (configuration, environment) => new TestS3StorageSettings(
                        new Uri(configuration["STORAGE_SERVER_URI"] + "/" + _bucketName),
                        new Uri(configuration["STORAGE_SERVER_URI"]),
                        _bucketName.ToString(), configuration["STORAGE_ACCESS_KEY"],
                        configuration["STORAGE_SECRET_KEY"]));
        }

        public override async ValueTask DisposeAsync()
        {
            var storage = Get<IStorage<TestS3StorageSettings>>();
            await storage.DeleteAllAsync();
            await base.DisposeAsync();
        }
    }
}
