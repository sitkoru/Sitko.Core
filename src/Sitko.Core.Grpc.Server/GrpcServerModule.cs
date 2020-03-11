using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.App.Helpers;
using Sitko.Core.Web;

namespace Sitko.Core.Grpc.Server
{
    public class GrpcServerModule : BaseApplicationModule<GrpcServerOptions>, IWebApplicationModule
    {
        public GrpcServerModule(GrpcServerOptions config, Application application) : base(config, application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddGrpc(options =>
            {
               options.EnableDetailedErrors = environment.IsDevelopment();
            });
            services.AddSingleton<GrpcServicesRegistrar>();
        }

        public void ConfigureEndpoints(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder, IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGrpcService<HealthService>();
        }

        public void ConfigureWebHostDefaults(IWebHostBuilder webHostBuilder)
        {
            webHostBuilder.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(0, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
                if (DockerHelper.IsRunningInDocker())
                {
                    options.ListenAnyIP(80, listenOptions => listenOptions.Protocols = HttpProtocols.Http1);
                }
                else
                {
                    var conf = options.ApplicationServices.GetService<IConfiguration>();
                    var urls = conf["ASPNETCORE_URLS"].Split(';');
                    foreach (string url in urls)
                    {
                        var uri = new Uri(url);
                        options.ListenAnyIP(uri.Port, listenOptions =>
                        {
                            listenOptions.Protocols =
                                uri.Scheme == "http" ? HttpProtocols.Http1 : HttpProtocols.Http1AndHttp2;
                            if (uri.Scheme == "https")
                            {
                                listenOptions.UseHttps();
                            }
                        });
                    }
                }

                Application.Set("grpcServer", true);
            });
        }
    }
}
