using System.Collections.Generic;
using System.Reflection;

namespace Sitko.Core.MessageBus
{
    public class MessageBusModuleConfig<TAssembly>
    {
        public List<Assembly> Assemblies { get; } = new List<Assembly> {typeof(TAssembly).Assembly};

        public void AddAssemblies(params Assembly[] assemblies)
        {
            Assemblies.AddRange(assemblies);
        }
    }
}
