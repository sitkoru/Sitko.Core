using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Sitko.Core.App;
using Sitko.Core.App.Web;

namespace Sitko.Core.Swagger
{
    public class SwaggerModule : BaseApplicationModule<SwaggerModuleConfig>, IWebApplicationModule
    {
        public SwaggerModule(SwaggerModuleConfig config, Application application) : base(config, application)
        {
        }

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
                        {new OpenApiSecurityScheme {Name = "Bearer"}, new string[] { }}
                    };
                    c.AddSecurityRequirement(security);
                }
            });
        }

        public void ConfigureAfterUseRouting(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
            appBuilder.UseSwaggerAuthorized($"{Config.Title} ({Config.Version})", "v1/swagger.json");
        }
    }

    public class SwaggerModuleConfig
    {
        public string Title { get; set; } = "App";
        public string Version { get; set; } = "v1";

        public bool EnableTokenAuth { get; set; } = true;
    }
}
