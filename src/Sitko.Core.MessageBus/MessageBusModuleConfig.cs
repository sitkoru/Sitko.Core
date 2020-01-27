using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sitko.Core.MessageBus
{
    public class MessageBusModuleConfig
    {
        public Assembly[] Assemblies { get; }

        public MessageBusModuleConfig(IEnumerable<Assembly> assemblies)
        {
            Assemblies = assemblies.ToArray();
        }
    }
}
