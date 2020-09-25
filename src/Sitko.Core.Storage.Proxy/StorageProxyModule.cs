using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using Sitko.Core.App;
using Sitko.Core.Storage.Proxy.ImageSharp;
using Sitko.Core.Storage.Proxy.StaticFiles;
using Sitko.Core.App.Web;
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
        private FileExtensionContentTypeProvider? _mimeTypeProvider;

        public StorageProxyModule(StorageProxyModuleConfig config, Application application) : base(config, application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddImageSharp(options =>
                {
                    options.Configuration = SixLabors.ImageSharp.Configuration.Default;
                    options.BrowserMaxAge = TimeSpan.FromDays(1);
                    options.CachedNameLength = 12;
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

            services.Configure<StorageFileOptions>(options =>
            {
                options.OnPrepareResponse = ctx =>
                {
                    var headers = ctx.Context.Response.Headers;
                    var contentType = headers["Content-Type"];

                    if (contentType != "application/x-gzip" && ctx.File.FilePath != null &&
                        !ctx.File.FilePath.EndsWith(".gz"))
                    {
                        return;
                    }

                    var fileNameToTry = ctx.File.FilePath?.Substring(0, ctx.File.FilePath.Length - 3);

                    if (_mimeTypeProvider != null &&
                        _mimeTypeProvider.TryGetContentType(fileNameToTry, out var mimeType))
                    {
                        headers.Add("Content-Encoding", "gzip");
                        headers["Content-Type"] = mimeType;
                    }

                    headers[HeaderNames.CacheControl] = "public,max-age=" + Config.MaxAgeHeader.TotalSeconds;
                };
            });
        }

        public void ConfigureBeforeUseRouting(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
            appBuilder.UseImageSharp();
            _mimeTypeProvider = new FileExtensionContentTypeProvider();
            appBuilder.UseMiddleware<StorageMiddleware<TStorageOptions>>();
        }
    }

    public class StorageProxyModuleConfig
    {
        public Action<ImageSharpMiddlewareOptions>? ConfigureImageSharpMiddleware { get; set; }
        public string? ImageSharpCacheDir { get; set; }
        public TimeSpan MaxAgeHeader { get; set; } = TimeSpan.FromDays(30);
    }
}
