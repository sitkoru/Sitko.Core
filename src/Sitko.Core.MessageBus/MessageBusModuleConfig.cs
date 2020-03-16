using System.Reflection;

namespace Sitko.Core.MessageBus
{
    public class MessageBusModuleConfig
    {
        public Assembly[] Assemblies { get; private set; } = new Assembly[0];

        public void SetAssemblies(params Assembly[] assemblies)
        {
            Assemblies = assemblies;
        }
    }
}
