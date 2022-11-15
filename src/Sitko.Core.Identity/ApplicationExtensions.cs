using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Sitko.Core.App;

namespace Sitko.Core.Identity;

public static class ApplicationExtensions
{
    public static Application AddIdentity<TUser, TRole, TPk, TDbContext>(this Application application,
        Action<IApplicationContext, IdentityModuleOptions> configure, string? optionsKey = null)
        where TUser : IdentityUser<TPk>
        where TRole : IdentityRole<TPk>
        where TPk : IEquatable<TPk>
        where TDbContext : IdentityDbContext<TUser, TRole, TPk> =>
        application.AddModule<IdentityModule<TUser, TRole, TPk, TDbContext>, IdentityModuleOptions>(
            configure, optionsKey);

    public static Application AddIdentity<TUser, TRole, TPk, TDbContext>(this Application application,
        Action<IdentityModuleOptions>? configure = null, string? optionsKey = null)
        where TUser : IdentityUser<TPk>
        where TRole : IdentityRole<TPk>
        where TPk : IEquatable<TPk>
        where TDbContext : IdentityDbContext<TUser, TRole, TPk> =>
        application.AddModule<IdentityModule<TUser, TRole, TPk, TDbContext>, IdentityModuleOptions>(
            configure, optionsKey);
}

