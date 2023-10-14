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
        hostApplicationBuilder.GetSitkoCore().AddImgProxy(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddImgProxy(this IHostApplicationBuilder hostApplicationBuilder,
        Action<ImgProxyModuleOptions>? configure = null,
        string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddImgProxy(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddImgProxy(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, ImgProxyModuleOptions> configure,
        string? optionsKey = null) => applicationBuilder
        .AddModule<ImgProxyModule, ImgProxyModuleOptions>(
            configure, optionsKey);

    public static ISitkoCoreApplicationBuilder AddImgProxy(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<ImgProxyModuleOptions>? configure = null,
        string? optionsKey = null) => applicationBuilder
        .AddModule<ImgProxyModule, ImgProxyModuleOptions>(
            configure, optionsKey);
}
