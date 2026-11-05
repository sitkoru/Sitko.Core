using System.Net;
using FluentValidation;
using Sitko.Core.App;
using StackExchange.Redis;

namespace Sitko.Core.Redis
{
    public class RedisModuleOptions : BaseModuleOptions
    {
        public RedisConnectionMode Mode { get; set; }
        public string? Configuration { get; set; }
        public string? SentinelConfiguration { get; set; }

        internal (ConfigurationOptions Data, ConfigurationOptions? Sentinel) ParseConfiguration()
        {
            if (!Enum.IsDefined(Mode))
            {
                throw new ArgumentException("Redis connection mode must be Direct or Sentinel");
            }

            var data = Parse(Configuration, "Redis:Connection:Configuration");
            if (Mode == RedisConnectionMode.Direct)
            {
                if (data.EndPoints.Count == 0 || !string.IsNullOrEmpty(data.ServiceName))
                {
                    throw new ArgumentException("Direct Redis requires endpoints and forbids serviceName");
                }

                return (data, null);
            }

            if (string.IsNullOrWhiteSpace(data.ServiceName))
            {
                throw new ArgumentException("Sentinel Redis requires serviceName in Configuration");
            }

            var sentinel = Parse(SentinelConfiguration, "Redis:Connection:SentinelConfiguration");
            if (sentinel.EndPoints.Count == 0)
            {
                throw new ArgumentException("SentinelConfiguration requires endpoints");
            }

            return (data, sentinel);
        }

        private static ConfigurationOptions Parse(string? configuration, string key)
        {
            try
            {
                var options = ConfigurationOptions.Parse(configuration ?? "");
                if (options.EndPoints.OfType<DnsEndPoint>().Any(static endpoint => endpoint.Host.Contains('/')))
                {
                    throw new ArgumentException("Redis endpoints must be host names or IP addresses");
                }

                return options;
            }
            catch (ArgumentException)
            {
                // Parser errors may contain credentials from the supplied configuration.
                throw new ArgumentException($"Invalid {key}");
            }
        }
    }

    public class RedisModuleOptionsValidator : AbstractValidator<RedisModuleOptions>
    {
        public RedisModuleOptionsValidator()
        {
            _ = RuleFor(static o => o.Mode).IsInEnum();
            _ = RuleFor(static o => o).Custom(static (options, context) =>
            {
                try
                {
                    _ = options.ParseConfiguration();
                }
                catch (ArgumentException exception)
                {
                    context.AddFailure(nameof(RedisModuleOptions.Configuration), exception.Message);
                }
            });
        }
    }

    public enum RedisConnectionMode
    {
        Direct,
        Sentinel
    }
}
