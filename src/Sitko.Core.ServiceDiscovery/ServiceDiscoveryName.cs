using System.Security.Cryptography;
using System.Text;

namespace Sitko.Core.ServiceDiscovery
{
    public static class ServiceDiscoveryName
    {
        public static string Create(string environment, string contract)
        {
            var identity = $"{environment.Length}:{environment}{contract}";
            var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
            return $"grpc-{hash[..58]}";
        }
    }
}
