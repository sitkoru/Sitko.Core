using System;
using System.IO;
using System.Threading.Tasks;
using Sitko.Core.Xunit;

namespace Sitko.Core.Storage.FileSystem.Tests
{
    public class BaseFileSystemStorageTestScope : BaseTestScope
    {
        private string _folder = Path.GetTempPath() + "/" + Guid.NewGuid();

        protected override TestApplication ConfigureApplication(TestApplication application, string name)
        {
            return base.ConfigureApplication(application, name)
                .AddModule<FileSystemStorageModule<TestFileSystemStorageSettings>, TestFileSystemStorageSettings>(
                    (configuration, environment, moduleConfig) =>
                    {
                        moduleConfig.PublicUri = new Uri(_folder);
                        moduleConfig.StoragePath = _folder;
                    });
        }

        public override async ValueTask DisposeAsync()
        {
            var storage = Get<IStorage<TestFileSystemStorageSettings>>();
            await storage.DeleteAllAsync();
            await base.DisposeAsync();
        }
    }
}
