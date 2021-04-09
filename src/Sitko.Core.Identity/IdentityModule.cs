using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.App.Web;

namespace Sitko.Core.Identity
{
    public class IdentityModule<TUser, TRole, TPk, TDbContext> : BaseApplicationModule<IdentityModuleOptions>,
        IWebApplicationModule
        where TUser : IdentityUser<TPk>
        where TRole : IdentityRole<TPk>
        where TDbContext : IdentityDbContext<TUser, TRole, TPk>
        where TPk : IEquatable<TPk>
    {
        public IdentityModule(IdentityModuleOptions config, Application application) : base(config, application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            var identityBuilder = services
                .AddIdentity<TUser, TRole>(options =>
                    options.SignIn.RequireConfirmedAccount = Config.RequireConfirmedAccount)
                .AddEntityFrameworkStores<TDbContext>()
                .AddErrorDescriber<RussianIdentityErrorDescriber>()
                .AddDefaultTokenProviders();
            if (Config.AddDefaultUi)
            {
                identityBuilder.AddDefaultUI();
                services.AddRazorPages();
            }

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = Config.LoginPath;
                options.LogoutPath = Config.LogoutPath;
                options.ExpireTimeSpan = Config.CookieExpireTimeSpan;
                options.SlidingExpiration = Config.CookieSlidingExpiration;
            });
        }

        public void ConfigureEndpoints(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder,
            IEndpointRouteBuilder endpoints)
        {
            if (Config.AddDefaultUi)
            {
                endpoints.MapRazorPages();
            }
        }

        public void ConfigureAfterUseRouting(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
            appBuilder.UseAuthentication();
            appBuilder.UseAuthorization();
        }
    }

    public class IdentityModuleOptions
    {
        public bool AddDefaultUi { get; set; }
        public TimeSpan CookieExpireTimeSpan { get; set; } = TimeSpan.FromDays(30);
        public bool CookieSlidingExpiration { get; set; } = true;
        public bool RequireConfirmedAccount { get; set; } = true;

        public string LoginPath { get; set; } = "/Identity/Account/Login";

        public string LogoutPath { get; set; } = "/Identity/Account/Logout";
    }
}
