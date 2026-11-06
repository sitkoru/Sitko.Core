using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Sitko.Core.ServiceDiscovery
{
    public static partial class ServiceDiscoveryName
    {
        public static string Create(string environment, string contract)
        {
            var shortContract = contract[(contract.LastIndexOf('.') + 1)..];
            if (shortContract.EndsWith("Service", StringComparison.Ordinal))
            {
                shortContract = shortContract[..^7];
            }

            var name = $"grpc-{Normalize(environment)}-{Normalize(shortContract)}".TrimEnd('-');
            if (name.Length > 50)
            {
                name = name[..50].TrimEnd('-');
            }

            var identity = $"{environment.Length}:{environment}{contract}";
            var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
            return $"{name}-{hash[..12]}";
        }

        private static string Normalize(string value) =>
            InvalidDnsCharacters().Replace(JsonNamingPolicy.KebabCaseLower.ConvertName(value), "-").Trim('-');

        [GeneratedRegex("[^a-z0-9]+")]
        private static partial Regex InvalidDnsCharacters();
    }
}
