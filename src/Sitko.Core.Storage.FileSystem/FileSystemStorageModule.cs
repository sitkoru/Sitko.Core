using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Storage.FileSystem
{
    public class
        FileSystemStorageModule<TStorageOptions> : StorageModule<FileSystemStorage<TStorageOptions>, TStorageOptions>
        where TStorageOptions : StorageOptions, IFileSystemStorageOptions, new()
    {
        public override string GetOptionsKey()
        {
            return $"Storage:FileSystem:{typeof(TStorageOptions).Name}";
        }

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            TStorageOptions startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddSingleton<IStorage<TStorageOptions>, FileSystemStorage<TStorageOptions>>();
        }
    }
}
