using Microsoft.Extensions.Hosting;

namespace Sitko.Core.App;

public class ApplicationLifetimeService : BackgroundService
{
    /*private readonly Application application;
    private readonly IApplicationContext applicationContext;
    private readonly IHostApplicationLifetime hostApplicationLifetime;
    private readonly IServiceProvider serviceProvider;

    public ApplicationLifetimeService(IHostApplicationLifetime hostApplicationLifetime,
        IServiceProvider serviceProvider, Application application, IApplicationContext applicationContext)
    {
        this.hostApplicationLifetime = hostApplicationLifetime;
        this.serviceProvider = serviceProvider;
        this.application = application;
        this.applicationContext = applicationContext;
    }*/

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // ReSharper disable once AsyncVoidLambda
        // hostApplicationLifetime.ApplicationStarted.Register(async () =>
        //     await application.OnStarted(applicationContext, serviceProvider));
        // // ReSharper disable once AsyncVoidLambda
        // hostApplicationLifetime.ApplicationStopping.Register(async () =>
        //     await application.OnStopping(applicationContext, serviceProvider));
        // // ReSharper disable once AsyncVoidLambda
        // hostApplicationLifetime.ApplicationStopped.Register(async () =>
        //     await application.OnStopped(applicationContext, serviceProvider));

        return Task.CompletedTask;
    }
}

