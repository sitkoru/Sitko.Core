using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.Email
{
    public class DevEmailModule : EmailModule<DevEmailModuleConfig>
    {
        protected override void ConfigureBuilder(FluentEmailServicesBuilder builder)
        {
        }
    }

    public class DevEmailModuleConfig : EmailModuleConfig
    {
        public DevEmailModuleConfig() : base("dev@localhost", "localhost", "http")
        {
        }
    }
}
