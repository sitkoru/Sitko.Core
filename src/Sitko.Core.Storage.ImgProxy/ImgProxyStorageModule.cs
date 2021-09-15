using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.ImgProxy;

namespace Sitko.Core.Storage.ImgProxy
{
    public class
        ImgProxyStorageModule<TStorageOptions> : BaseApplicationModule
        where TStorageOptions : StorageOptions
    {
        public override string OptionsKey => $"Storage:ImgProxy:{typeof(TStorageOptions).Name}";

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            BaseApplicationModuleOptions startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddSingleton<IImgProxyUrlGenerator<TStorageOptions>, ImgProxyUrlGenerator<TStorageOptions>>();
        }

        public override IEnumerable<Type> GetRequiredModules(ApplicationContext context,
            BaseApplicationModuleOptions options) => new[] { typeof(IStorageModule), typeof(ImgProxyModule) };
    }
}
