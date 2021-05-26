using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Storage.FileSystem
{
    public class
        FileSystemStorageModule<TStorageOptions> : StorageModule<FileSystemStorage<TStorageOptions>, TStorageOptions>
        where TStorageOptions : StorageOptions, IFileSystemStorageOptions, new()
    {
        public FileSystemStorageModule(Application application) : base(application)
        {
        }
        
        public override string GetConfigKey()
        {
            return $"Storage:FileSystem:{typeof(TStorageOptions).Name}";
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddSingleton<IStorage<TStorageOptions>, FileSystemStorage<TStorageOptions>>();
        }
    }
}
