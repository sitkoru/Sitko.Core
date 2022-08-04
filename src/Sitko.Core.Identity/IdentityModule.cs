using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.App.Web;

namespace Sitko.Core.Identity;

public class IdentityModule<TUser, TRole, TPk, TDbContext> : BaseApplicationModule<IdentityModuleOptions>,
    IWebApplicationModule
    where TUser : IdentityUser<TPk>
    where TRole : IdentityRole<TPk>
    where TDbContext : IdentityDbContext<TUser, TRole, TPk>
    where TPk : IEquatable<TPk>
{
    public override string OptionsKey => "Identity";

    public void ConfigureEndpoints(IApplicationContext applicationContext,
        IApplicationBuilder appBuilder,
        IEndpointRouteBuilder endpoints)
    {
        var config = GetOptions(appBuilder.ApplicationServices);
        if (config.AddDefaultUi)
        {
            endpoints.MapRazorPages();
        }
    }

    public void ConfigureAfterUseRouting(IApplicationContext applicationContext,
        IApplicationBuilder appBuilder)
    {
        appBuilder.UseAuthentication();
        appBuilder.UseAuthorization();
    }

    public override void ConfigureServices(IApplicationContext context, IServiceCollection services,
        IdentityModuleOptions startupOptions)
    {
        base.ConfigureServices(context, services, startupOptions);

        var identityBuilder =
            services
                .AddIdentity<TUser, TRole>(options =>
                    options.SignIn.RequireConfirmedAccount = startupOptions.RequireConfirmedAccount)
                .AddEntityFrameworkStores<TDbContext>()
                .AddErrorDescriber<RussianIdentityErrorDescriber>()
                .AddDefaultTokenProviders();
        if (startupOptions.AddDefaultUi)
        {
            var fakeEnvAdded = false;
            var webEnv = services.LastOrDefault(d => d.ServiceType == typeof(IWebHostEnvironment));

            if (webEnv is null)
            {
                if (services.FirstOrDefault(d => d.ServiceType == typeof(IHostEnvironment))?.ImplementationInstance is
                    IHostEnvironment env)
                {
                    services.AddSingleton<IWebHostEnvironment>(new FakeEnv
                    {
                        ApplicationName = env.ApplicationName,
                        EnvironmentName = env.EnvironmentName,
                        ContentRootPath = env.ContentRootPath
                    });

                    fakeEnvAdded = true;
                }
            }

            services.AddRazorPages();
            identityBuilder.AddDefaultUI();
            if (fakeEnvAdded)
            {
                services.Remove(services.Last(d => d.ServiceType == typeof(IWebHostEnvironment)));
            }
        }

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = startupOptions.LoginPath;
            options.LogoutPath = startupOptions.LogoutPath;
            options.ExpireTimeSpan = TimeSpan.FromDays(startupOptions.CookieExpireDays);
            options.SlidingExpiration = startupOptions.CookieSlidingExpiration;
        });
    }
}

public class IdentityModuleOptions : BaseModuleOptions
{
    public bool AddDefaultUi { get; set; }
    public int CookieExpireDays { get; set; } = 30;
    public bool CookieSlidingExpiration { get; set; } = true;
    public bool RequireConfirmedAccount { get; set; } = true;

    public string LoginPath { get; set; } = "/Identity/Account/Login";

    public string LogoutPath { get; set; } = "/Identity/Account/Logout";
}

public class FakeEnv : IWebHostEnvironment
{
    public string ApplicationName { get; set; }
    public IFileProvider ContentRootFileProvider { get; set; }
    public string ContentRootPath { get; set; }
    public string EnvironmentName { get; set; }
    public string WebRootPath { get; set; }
    public IFileProvider WebRootFileProvider { get; set; }
}
