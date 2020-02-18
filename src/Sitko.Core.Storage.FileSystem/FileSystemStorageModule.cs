using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Sitko.Core.Storage.FileSystem
{
    public class FileSystemStorageModule<T> : StorageModule<FileSystemStorage<T>, T>
        where T : StorageOptions, IFileSystemStorageOptions
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddSingleton<IStorage<T>, FileSystemStorage<T>>();
        }
    }
}
