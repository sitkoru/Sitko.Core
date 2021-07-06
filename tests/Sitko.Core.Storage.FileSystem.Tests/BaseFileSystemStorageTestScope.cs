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
            base.ConfigureApplication(application, name);
            application.AddFileSystemStorage<TestFileSystemStorageSettings>(
                moduleOptions =>
                {
                    moduleOptions.PublicUri = new Uri(_folder);
                    moduleOptions.StoragePath = _folder;
                });
            application.AddFileSystemStorageMetadata<TestFileSystemStorageSettings>();
            return application;
        }

        public override async ValueTask DisposeAsync()
        {
            var storage = Get<IStorage<TestFileSystemStorageSettings>>();
            await storage.DeleteAllAsync();
            await base.DisposeAsync();
        }
    }
}
