using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sitko.Core.App;
using Sitko.Core.Consul;
using Sitko.Core.Infrastructure.Helpers;

namespace Sitko.Core.Web
{
    public class ConsulWebModule : BaseApplicationModule<ConsulWebModuleConfig>, IWebApplicationModule
    {
        public override List<Type> GetRequiredModules()
        {
            return new List<Type> {typeof(ConsulModule)};
        }

        public async Task ApplicationStarted(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
            var server = appBuilder.ApplicationServices.GetRequiredService<IServer>();
            var consulClient = appBuilder.ApplicationServices.GetRequiredService<IConsulClient>();
            var logger = appBuilder.ApplicationServices.GetRequiredService<ILogger<ConsulWebModule>>();
            Uri uri;
            if (DockerHelper.IsRunningInDocker())
            {
                var host = DockerHelper.GetContainerAddress();
                if (!string.IsNullOrEmpty(configuration["VIRTUAL_HOST"]))
                {
                    host = configuration["VIRTUAL_HOST"];
                }

                var port = environment.IsProduction() ? 443 : 80;
                if (!string.IsNullOrEmpty(configuration["VIRTUAL_PORT"]))
                {
                    int.TryParse(configuration["VIRTUAL_PORT"], out port);
                }

                var proto = environment.IsProduction() ? "https" : "http";

                uri = new Uri($"{proto}://" + host + $":{port}");
            }
            else
            {
                var grpcServer = ApplicationStore.Get<bool>("grpcServer");
                var addressesFeature = server.Features.Get<IServerAddressesFeature>();
                var address = grpcServer
                    ? addressesFeature.Addresses.Skip(1).FirstOrDefault(a => !a.StartsWith("https"))
                    : addressesFeature.Addresses.FirstOrDefault(a => !a.StartsWith("https"));
                if (string.IsNullOrEmpty(address)) address = addressesFeature.Addresses.First();
                uri = new Uri(address.Replace("localhost", Config.IpAddress)
                    .Replace("0.0.0.0", Config.IpAddress).Replace("[::]", Config.IpAddress));
            }

            // Register service with consul
            var healthUrl = $"{uri.Scheme}://{uri.Host}:{uri.Port}/health";
            var registration = new AgentServiceRegistration
            {
                ID = environment.ApplicationName,
                Name = environment.ApplicationName,
                Address = uri.Host,
                Port = uri.Port,
                Check = new AgentServiceCheck {HTTP = healthUrl, Interval = TimeSpan.FromSeconds(30)},
                Tags = new[] {"metrics", $"healthUrl:{healthUrl}"}
            };

            logger.LogInformation("Registering with Consul");
            await consulClient.Agent.ServiceDeregister(registration.ID);
            await consulClient.Agent.ServiceRegister(registration);
        }

        public async Task ApplicationStopping(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
            var consulClient = appBuilder.ApplicationServices.GetRequiredService<IConsulClient>();
            var logger = appBuilder.ApplicationServices.GetRequiredService<ILogger<ConsulWebModule>>();
            logger.LogInformation("Remove service from Consul");
            await consulClient.Agent.ServiceDeregister(environment.ApplicationName);
        }

        public Task ApplicationStopped(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
            return Task.CompletedTask;
        }

        public void ConfigureEndpoints(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder, IEndpointRouteBuilder endpoints)
        {
        }

        public void ConfigureBeforeUseRouting(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
        }

        public void ConfigureAfterUseRouting(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
        }

        public void ConfigureWebHostDefaults(IWebHostBuilder webHostBuilder)
        {
        }

        public void ConfigureWebHost(IWebHostBuilder webHostBuilder)
        {
        }
    }

    public class ConsulWebModuleConfig
    {
        public string IpAddress { get; set; }
    }
}
