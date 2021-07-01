using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.Blazor.AntDesignComponents;

namespace Sitko.Core.Apps.Blazor
{
    public class Startup : AntBlazorStartup
    {
        public Startup(IConfiguration configuration, IHostEnvironment environment) : base(configuration, environment)
        {
        }

        protected override void ConfigureAppServices(IServiceCollection services)
        {
            base.ConfigureAppServices(services);
            services.AddValidatorsFromAssemblyContaining<Startup>();
        }
        protected override void ConfigureAfterRoutingMiddleware(IApplicationBuilder app)
        {
            base.ConfigureAfterRoutingMiddleware(app);
            app.UseRequestLocalization(new RequestLocalizationOptions()
                .AddSupportedCultures("en-US", "ru")
                .AddSupportedUICultures("en-US", "ru"));
        }
    }
}
