using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.App;

internal sealed class CommandsModule : BaseApplicationModule
{
    public override string OptionsKey => "Commands";

    public override async Task<bool> OnBeforeRunAsync(Application application, IApplicationContext applicationContext,
        string[] args)
    {
        await base.OnBeforeRunAsync(application, applicationContext, args);
        if (args.Length > 0)
        {
            var commandName = args[0];
            applicationContext.Logger.LogInformation("Run command {CommandName}", commandName);
            switch (commandName)
            {
                case "check":
                    applicationContext.Logger.LogCritical("Check run is successful. Exit");
                    return false;
                case "generate-options":
                    applicationContext.Logger.LogInformation("Generate options");
                    var modulesOptions = application.GetModulesOptions();
                    applicationContext.Logger.LogInformation("Modules options:");
                    applicationContext.Logger.LogInformation("{Options}", JsonSerializer.Serialize(modulesOptions,
                        new JsonSerializerOptions { WriteIndented = true }));
                    return false;
            }
        }

        return true;
    }
}

