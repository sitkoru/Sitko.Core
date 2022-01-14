using System;
using JetBrains.Annotations;
using Sitko.Core.App;

namespace Sitko.Core.ImgProxy;

[PublicAPI]
public static class ApplicationExtensions
{
    public static Application AddImgProxy(this Application application,
        Action<IApplicationContext, ImgProxyModuleOptions> configure,
        string? optionsKey = null) => application
        .AddModule<ImgProxyModule, ImgProxyModuleOptions>(
            configure, optionsKey);

    public static Application AddImgProxy(this Application application,
        Action<ImgProxyModuleOptions>? configure = null,
        string? optionsKey = null) => application
        .AddModule<ImgProxyModule, ImgProxyModuleOptions>(
            configure, optionsKey);
}
