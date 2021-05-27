using System.Collections.Generic;
using System.Reflection;
using Sitko.Core.App;

namespace Sitko.Core.MediatR
{
    public class MediatRModuleConfig<TAssembly> : BaseModuleConfig
    {
        public List<Assembly> Assemblies { get; } = new List<Assembly> {typeof(TAssembly).Assembly};

        public void AddAssemblies(params Assembly[] assemblies)
        {
            Assemblies.AddRange(assemblies);
        }
    }
}
