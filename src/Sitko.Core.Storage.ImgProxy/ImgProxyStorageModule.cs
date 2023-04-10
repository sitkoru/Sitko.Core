using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.ImgProxy;

namespace Sitko.Core.Storage.ImgProxy;

public class
    ImgProxyStorageModule<TStorageOptions> : BaseApplicationModule
    where TStorageOptions : StorageOptions
{
    public override string OptionsKey => $"Storage:ImgProxy:{typeof(TStorageOptions).Name}";
    public override string[] OptionKeys => new[] { "Storage:ImgProxy:Default", OptionsKey };

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        BaseApplicationModuleOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.AddSingleton<IImgProxyUrlGenerator<TStorageOptions>, ImgProxyUrlGenerator<TStorageOptions>>();
    }

    public override IEnumerable<Type> GetRequiredModules(IApplicationContext applicationContext,
        BaseApplicationModuleOptions options) => new[] { typeof(IStorageModule), typeof(ImgProxyModule) };
}

