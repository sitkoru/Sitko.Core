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

namespace Sitko.Core.Consul.Web
{
    public class ConsulWebModule : BaseApplicationModule<ConsulWebModuleConfig>
    {
        public ConsulWebModule(ConsulWebModuleConfig config, Application application) : base(config, application)
        {
        }

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
            if (Config.ServiceUri != null)
            {
                uri = Config.ServiceUri;
            }
            else
            {
                var addressesFeature = server.Features.Get<IServerAddressesFeature>();
                var addresses = addressesFeature.Addresses.Select(a => new Uri(a.Replace("localhost", Config.IpAddress)
                        .Replace("0.0.0.0", Config.IpAddress).Replace("[::]", Config.IpAddress))).OrderBy(u => u.Port)
                    .ToList();
                if (!addresses.Any())
                {
                    throw new Exception("No addresses available for consul registration");
                }

                var address = addresses.FirstOrDefault(u => u.Scheme != "https");
                if (address == null) address = addresses.First();
                uri = address;
            }

            var healthUrl = $"{uri.Scheme}://{uri.Host}:{uri.Port}/health";
            var registration = new AgentServiceRegistration
            {
                ID = environment.ApplicationName,
                Name = environment.ApplicationName,
                Address = uri.Host,
                Port = uri.Port,
                Check = new AgentServiceCheck
                {
                    HTTP = healthUrl,
                    DeregisterCriticalServiceAfter = Config.DeregisterTimeout,
                    Interval = Config.ChecksInterval
                },
                Tags = new[] {"metrics", $"healthUrl:{healthUrl}", $"version:{Config.Version}"}
            };

            logger.LogInformation("Registering in Consul as {Name} on {Host}:{Port}", environment.ApplicationName,
                uri.Host, uri.Port);
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
        public string? IpAddress { get; set; }
        public string Version { get; set; } = "dev";

        public Uri? ServiceUri { get; set; }

        public TimeSpan ChecksInterval { get; set; } = TimeSpan.FromSeconds(60);
        public TimeSpan DeregisterTimeout { get; set; } = TimeSpan.FromSeconds(60);
    }
}
