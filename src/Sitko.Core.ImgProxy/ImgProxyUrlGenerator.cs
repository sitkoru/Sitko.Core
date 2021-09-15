using System;
using ImgProxy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sitko.Core.ImgProxy
{
    public class ImgProxyUrlGenerator : IImgProxyUrlGenerator
    {
        private readonly IOptionsMonitor<ImgProxyModuleOptions> optionsMonitor;
        private ImgProxyModuleOptions Options => optionsMonitor.CurrentValue;
        protected ILogger<ImgProxyUrlGenerator> Logger { get; }

        public ImgProxyUrlGenerator(IOptionsMonitor<ImgProxyModuleOptions> optionsMonitor,
            ILogger<ImgProxyUrlGenerator> logger)
        {
            this.optionsMonitor = optionsMonitor;
            Logger = logger;
        }

        private ImgProxyBuilder GetBuilder() =>
            ImgProxyBuilder.New.WithEndpoint(Options.Host)
                .WithCredentials(Options.Key, Options.Salt);

        public string Url(string url)
        {
            Logger.LogDebug("Build url to image {Url}", url);
            return BuildUrl(url);
        }

        public string Format(string url, string format)
        {
            Logger.LogDebug("Build url to image {Url} with format {Format}", url, format);
            return BuildUrl(url, builder => builder.WithFormat(format));
        }

        public string Preset(string url, string preset)
        {
            Logger.LogDebug("Build url to image {Url} with preset {Preset}", url, preset);
            return BuildUrl(url, builder => builder.WithPreset(preset));
        }

        public string Build(string url, Action<ImgProxyBuilder> build)
        {
            Logger.LogDebug("Build url to image {Url}", url);
            return BuildUrl(url, build);
        }

        public string Resize(string url, int width, int height, string type = "auto", bool enlarge = false, bool extend = false)
        {
            Logger.LogDebug(
                "Build url to resized image {Url}. Width: {Width}. Height: {Height}. Type: {Type}. Enlarge: {Enlarge}",
                url, width, height, type, enlarge);
            return BuildUrl(url, builder => builder.WithOptions(new global::ImgProxy.ResizeOption(type, width, height, enlarge, extend)));
        }

        private string BuildUrl(string url, Action<ImgProxyBuilder>? build = null)
        {
            if (Options.DisableProxy)
            {
                return url;
            }

            var builder = GetBuilder();
            build?.Invoke(builder);
            return builder.Build(url, Options.EncodeUrls);
        }
    }
}
