using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Sitko.Core.App.Logging;

namespace Sitko.Core.App;

internal class ApplicationLifecycle(
    IApplicationContext context,
    IServiceProvider provider,
    IEnumerable<ApplicationModuleRegistration> applicationModuleRegistrations,
    IBootLogger<ApplicationLifecycle> logger)
    : IApplicationLifecycle
{
    private readonly IReadOnlyList<ApplicationModuleRegistration> enabledModules =
        ModulesHelper.GetEnabledModuleRegistrations(context, applicationModuleRegistrations);

    public async Task StartingAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Application starting");
        await using var scope = provider.CreateAsyncScope();

        foreach (var enabledModule in enabledModules)
        {
            var shouldContinue = await enabledModule.GetInstance()
                .OnBeforeRunAsync(context, scope.ServiceProvider, cancellationToken);
            if (!shouldContinue)
            {
                Environment.Exit(0);
                return;
            }
        }

        logger.LogInformation("Check required modules");
        var modulesCheckSuccess = true;
        foreach (var registration in enabledModules)
        {
            var result =
                registration.CheckRequiredModules(context,
                    enabledModules.Select(r => r.Type).ToArray());
            if (!result.isSuccess)
            {
                foreach (var missingModule in result.missingModules)
                {
                    Log.Information("Required module {MissingModule} for module {Type} is not registered",
                        missingModule, registration.Type);
                }

                modulesCheckSuccess = false;
            }
        }

        if (!modulesCheckSuccess)
        {
            throw new InvalidOperationException("Check required modules failed");
        }

        logger.LogInformation("Init modules");

        foreach (var configurationModule in enabledModules.Select(module => module.GetInstance())
                     .OfType<IConfigurationModule>())
        {
            configurationModule.CheckConfiguration(context, scope.ServiceProvider);
        }

        foreach (var registration in enabledModules)
        {
            logger.LogInformation("Init module {Module}", registration.Type);
            await registration.InitAsync(context, scope.ServiceProvider, cancellationToken);
        }

        foreach (var enabledModule in enabledModules)
        {
            var shouldContinue =
                await enabledModule.GetInstance().OnAfterRunAsync(context, scope.ServiceProvider, cancellationToken);
            if (!shouldContinue)
            {
                Environment.Exit(0);
            }
        }
    }

    public async Task StartedAsync(CancellationToken cancellationToken)
    {
        foreach (var moduleRegistration in enabledModules)
        {
            try
            {
                await moduleRegistration.ApplicationStarted(context, provider, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error on application started hook in module {Module}: {ErrorText}",
                    moduleRegistration.Type,
                    ex.ToString());
            }
        }

        logger.LogInformation("Applicaiton started");
    }

    public async Task StoppingAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Applicaiton stopping");
        foreach (var moduleRegistration in enabledModules)
        {
            try
            {
                await moduleRegistration.ApplicationStopping(context, provider, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error on application stopping hook in module {Module}: {ErrorText}",
                    moduleRegistration.Type,
                    ex.ToString());
            }
        }
    }

    public async Task StoppedAsync(CancellationToken cancellationToken)
    {
        foreach (var moduleRegistration in enabledModules)
        {
            try
            {
                await moduleRegistration.ApplicationStopped(context, provider, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error on application stopped hook in module {Module}: {ErrorText}",
                    moduleRegistration.Type,
                    ex.ToString());
            }
        }

        logger.LogInformation("Applicaiton stopped");
    }
}
