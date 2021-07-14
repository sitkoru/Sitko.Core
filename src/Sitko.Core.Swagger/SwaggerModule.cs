using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Sitko.Core.App;
using Sitko.Core.App.Web;

namespace Sitko.Core.Swagger
{
    public class SwaggerModule : BaseApplicationModule<SwaggerModuleOptions>, IWebApplicationModule
    {
        public override string OptionsKey => "Swagger";

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            SwaggerModuleOptions startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo {Title = startupOptions.Title, Version = startupOptions.Version});
                if (startupOptions.EnableTokenAuth)
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
                        {new OpenApiSecurityScheme {Name = "Bearer"}, System.Array.Empty<string>()}
                    };
                    c.AddSecurityRequirement(security);
                }
            });
        }

        public void ConfigureAfterUseRouting(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
            var config = GetOptions(appBuilder.ApplicationServices);
            appBuilder.UseSwaggerAuthorized($"{config.Title} ({config.Version})", "v1/swagger.json");
        }
    }

    public class SwaggerModuleOptions : BaseModuleOptions
    {
        public string Title { get; set; } = "App";
        public string Version { get; set; } = "v1";

        public bool EnableTokenAuth { get; set; } = true;
    }
}
