using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.ImgProxy;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddImgProxy(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, ImgProxyModuleOptions> configure,
        string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddImgProxy(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddImgProxy(this IHostApplicationBuilder hostApplicationBuilder,
        Action<ImgProxyModuleOptions>? configure = null,
        string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddImgProxy(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static SitkoCoreApplicationBuilder AddImgProxy(this SitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, ImgProxyModuleOptions> configure,
        string? optionsKey = null) => applicationBuilder
        .AddModule<ImgProxyModule, ImgProxyModuleOptions>(
            configure, optionsKey);

    public static SitkoCoreApplicationBuilder AddImgProxy(this SitkoCoreApplicationBuilder applicationBuilder,
        Action<ImgProxyModuleOptions>? configure = null,
        string? optionsKey = null) => applicationBuilder
        .AddModule<ImgProxyModule, ImgProxyModuleOptions>(
            configure, optionsKey);
}
