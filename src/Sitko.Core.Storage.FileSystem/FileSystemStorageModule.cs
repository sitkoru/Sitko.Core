using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Storage.FileSystem
{
    public class FileSystemStorageModule<T> : BaseApplicationModule<T> where T : class, IFileSystemStorageOptions
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddSingleton<IStorage<T>, FileSystemStorage<T>>();
        }
    }
}
