using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.App;

internal sealed class CommandsModule : BaseApplicationModule
{
    public override string OptionsKey => "Commands";

    public override async Task<bool> OnBeforeRunAsync(IApplicationContext applicationContext,
        IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        await base.OnBeforeRunAsync(applicationContext, serviceProvider, cancellationToken);
        if (applicationContext.Args.Length > 0)
        {
            var commandName = applicationContext.Args[0];
            applicationContext.Logger.LogInformation("Run command {CommandName}", commandName);
            switch (commandName)
            {
                case "check":
                    applicationContext.Logger.LogCritical("Check run is successful. Exit");
                    return false;
                case "generate-options":
                    applicationContext.Logger.LogInformation("Generate options");
                    var modulesOptions =
                        GetModulesOptions(applicationContext,
                            serviceProvider.GetServices<ApplicationModuleRegistration>());
                    applicationContext.Logger.LogInformation("Modules options:");
                    applicationContext.Logger.LogInformation("{Options}", JsonSerializer.Serialize(modulesOptions,
                        new JsonSerializerOptions { WriteIndented = true }));
                    return false;
            }
        }

        return true;
    }

    private Dictionary<string, object> GetModulesOptions(IApplicationContext applicationContext,
        IEnumerable<ApplicationModuleRegistration> moduleRegistrations)
    {
        var modulesOptions = new Dictionary<string, object> { { OptionsKey, applicationContext.Options } };
        foreach (var moduleRegistration in moduleRegistrations)
        {
            var (configKey, options) = moduleRegistration.GetOptions(applicationContext);
            if (!string.IsNullOrEmpty(configKey))
            {
                var current = modulesOptions;
                var parts = configKey.Split(':');
                for (var i = 0; i < parts.Length; i++)
                {
                    if (i == parts.Length - 1)
                    {
                        current[parts[i]] =
                            JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(options))!;
                    }
                    else
                    {
                        if (current.TryGetValue(parts[i], out var value))
                        {
                            current = (Dictionary<string, object>)value;
                        }
                        else
                        {
                            var part = new Dictionary<string, object>();
                            current[parts[i]] = part;
                            current = part;
                        }
                    }
                }
            }
        }

        return modulesOptions;
    }
}
