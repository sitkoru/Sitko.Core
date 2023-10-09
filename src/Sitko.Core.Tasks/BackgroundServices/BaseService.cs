using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Tasks.BackgroundServices;

public abstract class BaseService : BackgroundService
{
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<BaseService> logger;
    protected abstract TimeSpan InitDelay { get; }
    protected abstract TimeSpan RunDelay { get; }

    protected BaseService(IServiceScopeFactory serviceScopeFactory, ILogger<BaseService> logger)
    {
        this.serviceScopeFactory = serviceScopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(InitDelay, stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            try
            {
                await ExecuteAsync(scope, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError("Error execute service {Service}: {Error}", GetType(), ex);
            }

            await Task.Delay(RunDelay, stoppingToken);
        }
    }

    protected abstract Task ExecuteAsync(AsyncServiceScope scope, CancellationToken stoppingToken);
}
