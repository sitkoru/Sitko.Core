using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Storage.Metadata
{
    public abstract class
        BaseStorageMetadataModule<TStorageOptions, TProvider, TProviderOptions> : BaseApplicationModule<
            TProviderOptions> where TStorageOptions : StorageOptions
        where TProvider : class, IStorageMetadataProvider<TStorageOptions, TProviderOptions>
        where TProviderOptions : StorageMetadataProviderOptions, new()
    {
        protected BaseStorageMetadataModule(Application application) : base(application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            services.AddSingleton<IStorageMetadataProvider<TStorageOptions>, TProvider>();
        }

        public override List<Type> GetRequiredModules()
        {
            return new() {typeof(IStorageModule)};
        }
    }
}
