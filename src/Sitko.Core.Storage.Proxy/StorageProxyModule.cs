using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using Sitko.Core.App;
using Sitko.Core.Storage.Proxy.ImageSharp;
using Sitko.Core.Storage.Proxy.StaticFiles;
using Sitko.Core.Web;
using SixLabors.ImageSharp.Web.Caching;
using SixLabors.ImageSharp.Web.Commands;
using SixLabors.ImageSharp.Web.DependencyInjection;
using SixLabors.ImageSharp.Web.Middleware;
using SixLabors.ImageSharp.Web.Processors;

namespace Sitko.Core.Storage.Proxy
{
    public class StorageProxyModule<TStorageOptions> : BaseApplicationModule<StorageProxyModuleConfig>,
        IWebApplicationModule where TStorageOptions : StorageOptions
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddImageSharpCore(options =>
                {
                    options.Configuration = SixLabors.ImageSharp.Configuration.Default;
                    options.MaxBrowserCacheDays = 1;
                    options.MaxCacheDays = 1;
                    options.CachedNameLength = 12;
                    options.OnParseCommands = _ => { };
                    options.OnBeforeSave = _ => { };
                    options.OnProcessed = _ => { };

                    options.OnPrepareResponse = _ => { };
                    Config.ConfigureImageSharpMiddleware?.Invoke(options);
                })
                .SetRequestParser<QueryCollectionRequestParser>()
                .Configure<PhysicalFileSystemCacheOptions>(options =>
                {
                    if (!string.IsNullOrEmpty(Config.ImageSharpCacheDir))
                    {
                        options.CacheFolder = Config.ImageSharpCacheDir;
                    }
                })
                .SetCache<PhysicalFileSystemCache>()
                .SetCacheHash<CacheHash>()
                .AddProvider<ImageSharpStorageProvider<TStorageOptions>>()
                .AddProcessor<ResizeWebProcessor>()
                .AddProcessor<BackgroundColorWebProcessor>()
                .AddProcessor<FormatWebProcessor>();

            services.AddSingleton<StorageFileProvider<TStorageOptions>>();
        }

        public void ConfigureBeforeUseRouting(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
            appBuilder.UseImageSharp();
            appBuilder.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = appBuilder.ApplicationServices
                    .GetRequiredService<StorageFileProvider<TStorageOptions>>(),
                OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers[HeaderNames.CacheControl] =
                        "public,max-age=" + Config.MaxAgeHeader.TotalSeconds;
                }
            });
        }
    }

    public class StorageProxyModuleConfig
    {
        public Action<ImageSharpMiddlewareOptions>? ConfigureImageSharpMiddleware { get; set; }
        public string? ImageSharpCacheDir { get; set; }
        public TimeSpan MaxAgeHeader { get; set; } = TimeSpan.FromDays(30);
    }
}
