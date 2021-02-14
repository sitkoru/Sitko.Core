using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Storage.FileSystem
{
    public class FileSystemStorageModule<T> : StorageModule<FileSystemStorage<T>, T>
        where T : StorageOptions, IFileSystemStorageOptions, new()
    {
        public FileSystemStorageModule(T config, Application application) : base(config, application)
        {
            if (config.ConfigureMetadata is null)
            {
                config.EnableMetadata<FileSystemStorageMetadataProvider<T>, FileSystemStorageMetadataProviderOptions>();
            }
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddSingleton<IStorage<T>, FileSystemStorage<T>>();
        }
    }
}
