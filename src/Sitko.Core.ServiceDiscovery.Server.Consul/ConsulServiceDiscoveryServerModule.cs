using Sitko.Core.App;
using Sitko.Core.Consul;

namespace Sitko.Core.ServiceDiscovery.Server.Consul;

public class ConsulServiceDiscoveryServerModule : ServiceDiscoveryServerModule<ConsulServiceDiscoveryServerModuleOptions
    , ConsulServiceDiscoveryRegistrar>
{
    public override IEnumerable<Type> GetRequiredModules(IApplicationContext applicationContext,
        ConsulServiceDiscoveryServerModuleOptions options) => [typeof(ConsulModule)];
}
