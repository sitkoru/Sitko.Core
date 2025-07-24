using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.App;

public interface IApplicationContext
{
    Guid Id { get; }
    string Name { get; }
    string Version { get; }
    ApplicationOptions Options { get; }
    IConfiguration Configuration { get; }
    ILogger Logger { get; }
    string Environment { get; }
    string AspNetEnvironmentName { get; }
    public string[] Args { get; }
    bool IsDevelopment();
    bool IsProduction();
    bool IsStaging();
    bool IsEnvironment(string environmentName);

    public T GetModuleInstance<T>() where T : class, IApplicationModule;
}
