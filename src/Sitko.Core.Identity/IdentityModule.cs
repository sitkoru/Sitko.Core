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
        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
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
                identityBuilder.AddDefaultUI();
                services.AddRazorPages();
            }

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = startupOptions.LoginPath;
                options.LogoutPath = startupOptions.LogoutPath;
                options.ExpireTimeSpan = TimeSpan.FromDays(startupOptions.CookieExpireDays);
                options.SlidingExpiration = startupOptions.CookieSlidingExpiration;
            });
        }

        public void ConfigureEndpoints(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder,
            IEndpointRouteBuilder endpoints)
        {
            var config = GetOptions(appBuilder.ApplicationServices);
            if (config.AddDefaultUi)
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

        public override string OptionsKey => "Identity";
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
}
