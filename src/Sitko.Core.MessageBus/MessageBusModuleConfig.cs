using System.Reflection;

namespace Sitko.Core.MessageBus
{
    public class MessageBusModuleConfig
    {
        public Assembly[] Assemblies { get; }

        public MessageBusModuleConfig(params Assembly[] assemblies)
        {
            Assemblies = assemblies;
        }
    }
}
