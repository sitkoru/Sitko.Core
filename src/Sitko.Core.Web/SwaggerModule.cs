using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Sitko.Core.App;

namespace Sitko.Core.Web
{
    public class SwaggerModule : BaseApplicationModule<SwaggerModuleConfig>, IWebApplicationModule
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo {Title = Config.Title, Version = Config.Version});
                if (Config.EnableTokenAuth)
                {
                    c.AddSecurityDefinition("Bearer",
                        new OpenApiSecurityScheme()
                        {
                            Description = "Auth token",
                            Name = "Authorization",
                            In = ParameterLocation.Header,
                            Type = SecuritySchemeType.ApiKey
                        });
                    var security = new OpenApiSecurityRequirement
                    {
                        {new OpenApiSecurityScheme() {Name = "Bearer"}, new string[] { }}
                    };
                    c.AddSecurityRequirement(security);
                }
            });
        }

        public Task ApplicationStarted(IApplicationBuilder appBuilder)
        {
            return Task.CompletedTask;
        }

        public Task ApplicationStopping(IApplicationBuilder appBuilder)
        {
            return Task.CompletedTask;
        }

        public Task ApplicationStopped(IApplicationBuilder appBuilder)
        {
            return Task.CompletedTask;
        }

        public void ConfigureEndpoints(IApplicationBuilder appBuilder, IEndpointRouteBuilder endpoints)
        {
        }

        public void ConfigureBeforeUseRouting(IApplicationBuilder appBuilder)
        {
        }

        public void ConfigureAfterUseRouting(IApplicationBuilder appBuilder)
        {
            appBuilder.UseSwaggerAuthorized($"{Config.Title} ({Config.Version})", "v1/swagger.json");
        }

        public void ConfigureWebHostDefaults(IWebHostBuilder webHostBuilder)
        {
        }

        public void ConfigureWebHost(IWebHostBuilder webHostBuilder)
        {
        }
    }

    public class SwaggerModuleConfig
    {
        public SwaggerModuleConfig(string title, string version)
        {
            Title = title;
            Version = version;
        }

        public string Title { get; }
        public string Version { get; }

        public bool EnableTokenAuth { get; set; } = true;
    }
}
