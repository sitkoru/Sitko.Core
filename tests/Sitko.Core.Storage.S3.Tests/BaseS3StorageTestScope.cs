using System;
using System.Threading.Tasks;
using Sitko.Core.Storage.Cache;
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
                    (configuration, environment, moduleConfig) =>
                    {
                        moduleConfig.PublicUri = new Uri(configuration["MINIO_SERVER_URI"] + "/" + _bucketName);
                        moduleConfig.Server = new Uri(configuration["MINIO_SERVER_URI"]);
                        moduleConfig.Bucket = _bucketName.ToString();
                        moduleConfig.AccessKey = configuration["MINIO_ACCESS_KEY"];
                        moduleConfig.SecretKey = configuration["MINIO_SECRET_KEY"];
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
