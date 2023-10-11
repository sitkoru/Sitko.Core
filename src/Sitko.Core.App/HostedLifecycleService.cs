using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Sitko.Core.App;

public class HostedLifecycleService : IHostedLifecycleService
{
    private readonly ILogger<HostedLifecycleService> logger;
    private readonly IApplicationContext applicationContext;
    private readonly IServiceProvider serviceProvider;

    private readonly IReadOnlyList<ApplicationModuleRegistration> enabledModules;

    public HostedLifecycleService(ILogger<HostedLifecycleService> logger, IApplicationContext applicationContext,
        IServiceProvider serviceProvider, IEnumerable<ApplicationModuleRegistration> applicationModuleRegistrations)
    {
        this.logger = logger;
        this.applicationContext = applicationContext;
        this.serviceProvider = serviceProvider;
        enabledModules =
            ModulesHelper.GetEnabledModuleRegistrations(applicationContext, applicationModuleRegistrations);
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StartingAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        foreach (var enabledModule in enabledModules)
        {
            var shouldContinue = await enabledModule.GetInstance()
                .OnBeforeRunAsync(applicationContext, scope.ServiceProvider);
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
                registration.CheckRequiredModules(applicationContext,
                    enabledModules.Select(r => r.Type).ToArray());
            if (!result.isSuccess)
            {
                foreach (var missingModule in result.missingModules)
                {
                    Log.Information(
                        $"Required module {missingModule} for module {registration.Type} is not registered");
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
            configurationModule.CheckConfiguration(applicationContext, scope.ServiceProvider);
        }

        foreach (var registration in enabledModules)
        {
            logger.LogInformation("Init module {Module}", registration.Type);
            await registration.InitAsync(applicationContext, scope.ServiceProvider);
        }

        foreach (var enabledModule in enabledModules)
        {
            var shouldContinue =
                await enabledModule.GetInstance().OnAfterRunAsync(applicationContext, scope.ServiceProvider);
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
                await moduleRegistration.ApplicationStarted(applicationContext, serviceProvider);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error on application started hook in module {Module}: {ErrorText}",
                    moduleRegistration.Type,
                    ex.ToString());
            }
        }
    }

    public async Task StoppingAsync(CancellationToken cancellationToken)
    {
        foreach (var moduleRegistration in enabledModules)
        {
            try
            {
                await moduleRegistration.ApplicationStopping(applicationContext, serviceProvider);
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
                await moduleRegistration.ApplicationStopped(applicationContext, serviceProvider);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error on application stopped hook in module {Module}: {ErrorText}",
                    moduleRegistration.Type,
                    ex.ToString());
            }
        }
    }
}
