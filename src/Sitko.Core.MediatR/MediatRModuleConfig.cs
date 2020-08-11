using System.Collections.Generic;
using System.Reflection;

namespace Sitko.Core.MediatR
{
    public class MediatRModuleConfig<TAssembly>
    {
        public List<Assembly> Assemblies { get; } = new List<Assembly> {typeof(TAssembly).Assembly};

        public void AddAssemblies(params Assembly[] assemblies)
        {
            Assemblies.AddRange(assemblies);
        }
    }
}
