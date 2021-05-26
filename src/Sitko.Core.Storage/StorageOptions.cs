using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Storage.Cache;
using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage
{
    public abstract class StorageOptions : BaseModuleConfig
    {
        public Uri? PublicUri { get; set; }

        public string? Prefix { get; set; }

        public abstract string Name { get; set; }

        // public Action<IHostEnvironment, IConfiguration, IServiceCollection>? ConfigureCache { get; protected set; }
        // public Action<IHostEnvironment, IConfiguration, IServiceCollection>? ConfigureMetadata { get; protected set; }

        // public StorageOptions EnableCache<TCache, TCacheOptions>(Action<TCacheOptions>? configure = null)
        //     where TCache : class, IStorageCache<TCacheOptions> where TCacheOptions : StorageCacheOptions
        // {
        //     ConfigureCache = (_, _, services) =>
        //     {
        //         var options = Activator.CreateInstance<TCacheOptions>();
        //         configure?.Invoke(options);
        //         services.AddSingleton(options);
        //         services.AddSingleton<IStorageCache, TCache>();
        //     };
        //     return this;
        // }



        public override (bool isSuccess, IEnumerable<string> errors) CheckConfig()
        {
            var result = base.CheckConfig();
            if (result.isSuccess)
            {
                if (PublicUri is null)
                {
                    return (false, new[] {"Storage url is empty"});
                }

                if (string.IsNullOrEmpty(Name))
                {
                    return (false, new[] {"Storage name is empty"});
                }
            }

            return result;
        }
    }
}
