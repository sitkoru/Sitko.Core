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
        public override string GetConfigKey()
        {
            return "Swagger";
        }

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            SwaggerModuleConfig startupConfig)
        {
            base.ConfigureServices(context, services, startupConfig);
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo {Title = startupConfig.Title, Version = startupConfig.Version});
                if (startupConfig.EnableTokenAuth)
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
            var config = GetConfig(appBuilder.ApplicationServices);
            appBuilder.UseSwaggerAuthorized($"{config.Title} ({config.Version})", "v1/swagger.json");
        }
    }

    public class SwaggerModuleConfig : BaseModuleConfig
    {
        public string Title { get; set; } = "App";
        public string Version { get; set; } = "v1";

        public bool EnableTokenAuth { get; set; } = true;
    }
}
