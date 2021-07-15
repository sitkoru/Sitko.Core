using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Storage.ImgProxy
{
    using JetBrains.Annotations;

    public class
        ImgProxyStorageModule<TStorageOptions> : BaseApplicationModule<ImgProxyStorageModuleOptions<TStorageOptions>>
        where TStorageOptions : StorageOptions
    {
        public override string OptionsKey => $"Storage:ImgProxy:{typeof(TStorageOptions).Name}";

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            ImgProxyStorageModuleOptions<TStorageOptions> startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddSingleton<IImgProxyUrlGenerator<TStorageOptions>, ImgProxyUrlGenerator<TStorageOptions>>();
        }

        public override IEnumerable<Type> GetRequiredModules(ApplicationContext context,
            ImgProxyStorageModuleOptions<TStorageOptions> options) =>
            new[] {typeof(IStorageModule)};
    }

    [PublicAPI]
    // Generic parameter is required for dependency injection
    // ReSharper disable once UnusedTypeParameter
    public class ImgProxyStorageModuleOptions<TStorageOptions> : BaseModuleOptions
        where TStorageOptions : StorageOptions
    {
        public string Host { get; set; } = "";
        public string Key { get; set; } = "";
        public string Salt { get; set; } = "";
        public bool EncodeUrls { get; set; }
    }
}
