using Microsoft.Extensions.Configuration;

namespace Sitko.Core.App.Logging;

public class SerilogDynamicConfigurationSource : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder) => SerilogDynamicConfigurationProvider.Instance;
}
