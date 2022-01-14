using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Sitko.Core.App;
using Sitko.Core.App.Web;

namespace Sitko.Core.Swagger;

public class SwaggerModule : BaseApplicationModule<SwaggerModuleOptions>, IWebApplicationModule
{
    public override string OptionsKey => "Swagger";

    public void ConfigureAfterUseRouting(IApplicationContext applicationContext,
        IApplicationBuilder appBuilder)
    {
        var config = GetOptions(appBuilder.ApplicationServices);
        appBuilder.UseSwagger();
        var endPoint = !string.IsNullOrEmpty(config.Endpoint) ? config.Endpoint : $"{config.Version}/swagger.json";
        appBuilder.UseSwaggerUI(c => { c.SwaggerEndpoint(endPoint, $"{config.Title} ({config.Version})"); });
    }

    public override void ConfigureServices(IApplicationContext context, IServiceCollection services,
        SwaggerModuleOptions startupOptions)
    {
        base.ConfigureServices(context, services, startupOptions);
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = startupOptions.Title, Version = startupOptions.Version });
            if (startupOptions.EnableTokenAuth)
            {
                c.AddSecurityDefinition("Bearer",
                    new OpenApiSecurityScheme
                    {
                        Description = "Auth token",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.ApiKey
                    });
                var security = new OpenApiSecurityRequirement
                {
                    { new OpenApiSecurityScheme { Name = "Bearer" }, Array.Empty<string>() }
                };
                c.AddSecurityRequirement(security);
            }
        });
    }
}

public class SwaggerModuleOptions : BaseModuleOptions
{
    public string Title { get; set; } = "App";
    public string Version { get; set; } = "v1";
    public string? Endpoint { get; set; }

    public bool EnableTokenAuth { get; set; } = true;
}
