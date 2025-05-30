using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
using Sitko.Core.Auth;

namespace Sitko.Core.Identity;

public class IdentityModule<TUser, TRole, TPk, TDbContext> : AuthModule<IdentityModuleOptions>,
    IAuthApplicationModule
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

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        IdentityModuleOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);

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
    }

    protected override void ConfigureCookieOptions(CookieAuthenticationOptions options,
        IdentityModuleOptions moduleOptions)
    {
        base.ConfigureCookieOptions(options, moduleOptions);
        options.LoginPath = moduleOptions.LoginPath;
        options.LogoutPath = moduleOptions.LogoutPath;
    }

    protected override void ConfigureAuthentication(AuthenticationBuilder authenticationBuilder,
        IdentityModuleOptions startupOptions)
    {
        // do nothing
    }
}

public class IdentityModuleOptions : AuthOptions
{
    public bool AddDefaultUi { get; set; }
    public bool RequireConfirmedAccount { get; set; } = true;
    public string LoginPath { get; set; } = "/Identity/Account/Login";
    public string LogoutPath { get; set; } = "/Identity/Account/Logout";
    public override bool RequiresCookie => true;
    public override bool RequiresAuthentication => false;
    public override string SignInScheme => IdentityConstants.ExternalScheme;
    public override string ChallengeScheme => IdentityConstants.ApplicationScheme;
}

public class FakeEnv : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "";
    public IFileProvider ContentRootFileProvider { get; set; } = null!;
    public string ContentRootPath { get; set; } = "";
    public string EnvironmentName { get; set; } = "";
    public string WebRootPath { get; set; } = "";
    public IFileProvider WebRootFileProvider { get; set; } = null!;
}
