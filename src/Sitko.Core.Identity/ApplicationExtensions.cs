using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Identity;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddIdentity<TUser, TRole, TPk, TDbContext>(
        this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, IdentityModuleOptions> configure, string? optionsKey = null)
        where TUser : IdentityUser<TPk>
        where TRole : IdentityRole<TPk>
        where TPk : IEquatable<TPk>
        where TDbContext : IdentityDbContext<TUser, TRole, TPk>
    {
        hostApplicationBuilder.AddSitkoCore().AddIdentity<TUser, TRole, TPk, TDbContext>(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddIdentity<TUser, TRole, TPk, TDbContext>(
        this IHostApplicationBuilder hostApplicationBuilder,
        Action<IdentityModuleOptions>? configure = null, string? optionsKey = null)
        where TUser : IdentityUser<TPk>
        where TRole : IdentityRole<TPk>
        where TPk : IEquatable<TPk>
        where TDbContext : IdentityDbContext<TUser, TRole, TPk>
    {
        hostApplicationBuilder.AddSitkoCore().AddIdentity<TUser, TRole, TPk, TDbContext>(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static SitkoCoreApplicationBuilder AddIdentity<TUser, TRole, TPk, TDbContext>(
        this SitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, IdentityModuleOptions> configure, string? optionsKey = null)
        where TUser : IdentityUser<TPk>
        where TRole : IdentityRole<TPk>
        where TPk : IEquatable<TPk>
        where TDbContext : IdentityDbContext<TUser, TRole, TPk> =>
        applicationBuilder.AddModule<IdentityModule<TUser, TRole, TPk, TDbContext>, IdentityModuleOptions>(
            configure, optionsKey);

    public static SitkoCoreApplicationBuilder AddIdentity<TUser, TRole, TPk, TDbContext>(
        this SitkoCoreApplicationBuilder applicationBuilder,
        Action<IdentityModuleOptions>? configure = null, string? optionsKey = null)
        where TUser : IdentityUser<TPk>
        where TRole : IdentityRole<TPk>
        where TPk : IEquatable<TPk>
        where TDbContext : IdentityDbContext<TUser, TRole, TPk> =>
        applicationBuilder.AddModule<IdentityModule<TUser, TRole, TPk, TDbContext>, IdentityModuleOptions>(
            configure, optionsKey);
}
