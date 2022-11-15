using System.Reflection;
using Sitko.Core.App;

namespace Sitko.Core.MediatR;

public class MediatRModuleOptions<TAssembly> : BaseModuleOptions
{
    public List<Assembly> Assemblies { get; } = new() { typeof(TAssembly).Assembly };

    public void AddAssemblies(params Assembly[] assemblies) => Assemblies.AddRange(assemblies);
}

