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
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = @"JWT Authorization header using the Bearer scheme. \r\n\r\n
                      Enter 'Bearer' [space] and then your token in the text input below.
                      \r\n\r\nExample: 'Bearer 12345abcdef'",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header,
                        },
                        Array.Empty<string>()
                    }
                });
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
