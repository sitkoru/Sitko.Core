using FluentValidation;
using Sitko.Core.App;

namespace Sitko.Core.Redis
{
    public class RedisModuleOptions : BaseModuleOptions
    {
        public string? Host { get; set; }
        public int Port { get; set; } = 6379;
        public int Db { get; set; }
        public string? Password { get; set; }
        public string? User { get; set; }
        public string[] SentinelEndpoints { get; set; } = [];
        public string? ServiceName { get; set; }
        public string? SentinelUser { get; set; }
        public string? SentinelPassword { get; set; }
        internal bool UseSentinel => ServiceName is not null || SentinelEndpoints is { Length: > 0 };
    }

    public class RedisModuleOptionsValidator : AbstractValidator<RedisModuleOptions>
    {
        public RedisModuleOptionsValidator()
        {
            _ = RuleFor(static o => o.Host).NotEmpty().When(static o => !o.UseSentinel)
                .WithMessage("Redis host can't be empty for a direct connection");
            _ = RuleFor(static o => o.Port).InclusiveBetween(1, 65535).When(static o => !o.UseSentinel);
            _ = RuleFor(static o => o.ServiceName).NotEmpty().When(static o => o.UseSentinel)
                .WithMessage("Redis service name is required for Sentinel");
            _ = RuleFor(static o => o.SentinelEndpoints).NotEmpty().When(static o => o.UseSentinel)
                .WithMessage("At least one Redis Sentinel endpoint is required");
            _ = RuleForEach(static o => o.SentinelEndpoints).Must(IsValidSentinelEndpoint)
                .WithMessage("Redis Sentinel endpoint must be a host:port address with a valid port");
        }

        private static bool IsValidSentinelEndpoint(string endpoint) =>
            Uri.TryCreate($"redis://{endpoint}", UriKind.Absolute, out var uri) &&
            uri.Port > 0 && uri.Host.Length > 0 && uri.UserInfo.Length == 0 &&
            uri.AbsolutePath == "/" && uri.Query.Length == 0 && uri.Fragment.Length == 0;
    }
}
