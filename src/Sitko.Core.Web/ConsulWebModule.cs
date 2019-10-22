using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Consul;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sitko.Core.App;
using Sitko.Core.App.Helpers;
using Sitko.Core.Consul;

namespace Sitko.Core.Web
{
    public class ConsulWebModule : BaseApplicationModule<ConsulWebModuleConfig>
    {
        public override List<Type> GetRequiredModules()
        {
            return new List<Type> {typeof(ConsulModule)};
        }

        public override async Task ApplicationStarted(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider)
        {
            var server = serviceProvider.GetRequiredService<IServer>();
            var consulClient = serviceProvider.GetRequiredService<IConsulClient>();
            var logger = serviceProvider.GetRequiredService<ILogger<ConsulWebModule>>();
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
                Tags = new[] {"metrics", $"healthUrl:{healthUrl}", $"version:{Config.Version}"}
            };

            logger.LogInformation("Registering with Consul");
            await consulClient.Agent.ServiceDeregister(registration.ID);
            await consulClient.Agent.ServiceRegister(registration);
        }

        public override async Task ApplicationStopping(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider)
        {
            var consulClient = serviceProvider.GetRequiredService<IConsulClient>();
            var logger = serviceProvider.GetRequiredService<ILogger<ConsulWebModule>>();
            logger.LogInformation("Remove service from Consul");
            await consulClient.Agent.ServiceDeregister(environment.ApplicationName);
        }
    }

    public class ConsulWebModuleConfig
    {
        public string IpAddress { get; set; }
        public string Version { get; set; } = "dev";
    }
}
