using JetBrains.Annotations;
using Sitko.Core.App;

namespace Sitko.Core.ServiceDiscovery.Server;

[PublicAPI]
public class ServiceDiscoveryModuleOptions : BaseModuleOptions
{
    public int RefreshIntervalInSeconds { get; set; } = 15;
}
