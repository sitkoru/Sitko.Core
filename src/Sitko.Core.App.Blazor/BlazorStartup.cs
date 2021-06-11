using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App.Blazor.Components;
using Sitko.Core.App.Blazor.Forms;
using Sitko.Core.App.Web;

namespace Sitko.Core.App.Blazor
{
    public class BlazorStartup : BaseStartup
    {
        public BlazorStartup(IConfiguration configuration, IHostEnvironment environment) : base(configuration,
            environment)
        {
        }

        protected override void ConfigureAppServices(IServiceCollection services)
        {
            base.ConfigureAppServices(services);
            services.AddRazorPages();
            services.AddServerSideBlazor().AddCircuitOptions(options =>
            {
                options.DetailedErrors = Environment.IsDevelopment();
            });
            AddForms(services, GetType().Assembly);
        }

        protected void AddForms<TAssembly>(IServiceCollection services)
        {
            AddForms(services, typeof(TAssembly).Assembly);
        }

        protected void AddForms(IServiceCollection services, Assembly assembly)
        {
            services.Scan(s =>
                s.FromAssemblies(assembly).AddClasses(c => c.AssignableTo<BaseForm>()).AsSelf()
                    .WithTransientLifetime());
        }

        protected void ForceAuthorization(IServiceCollection services)
        {
            services.AddRazorPages(options =>
            {
                options.Conventions.AuthorizeFolder("/");
            });
        }

        protected override void ConfigureEndpoints(IApplicationBuilder app, IEndpointRouteBuilder endpoints)
        {
            base.ConfigureEndpoints(app, endpoints);
            endpoints.MapBlazorHub();
            endpoints.MapFallbackToPage("/_Host");
        }
    }
}
