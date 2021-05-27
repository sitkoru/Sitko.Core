using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Storage.Metadata
{
    public abstract class
        BaseStorageMetadataModule<TStorageOptions, TProvider, TProviderOptions> : BaseApplicationModule<
            TProviderOptions> where TStorageOptions : StorageOptions
        where TProvider : class, IStorageMetadataProvider<TStorageOptions, TProviderOptions>
        where TProviderOptions : StorageMetadataProviderOptions, new()
    {
        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            TProviderOptions startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddSingleton<IStorageMetadataProvider<TStorageOptions>, TProvider>();
        }

        public override IEnumerable<Type> GetRequiredModules(ApplicationContext context, TProviderOptions config)
        {
            return new[] {typeof(IStorageModule)};
        }
    }
}
