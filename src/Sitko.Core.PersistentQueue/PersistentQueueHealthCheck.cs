using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.PersistentQueue
{
    public class PersistentQueueHealthCheck<TConnection> : IHealthCheck where TConnection : IPersistentQueueConnection
    {
        private readonly IPersistentQueueConnectionFactory<TConnection> _connectionFactory;

        private readonly ILogger<PersistentQueueHealthCheck<TConnection>> _logger;

        public PersistentQueueHealthCheck(
            IPersistentQueueConnectionFactory<TConnection> connectionFactory,
            ILogger<PersistentQueueHealthCheck<TConnection>> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var connections = _connectionFactory.GetCurrentConnections();
            var hasErrors = false;
            if (connections.Any())
            {
                foreach (TConnection connection in connections)
                {
                    if (!connection.IsHealthy())
                    {
                        hasErrors = true;
                        _logger.LogError("Check pq connection: Fail {connectionId}. Last error: {errorText}",
                            connection.Id,
                            connection.GetLastError());
                    }
                }
            }

            if (hasErrors)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy());
            }

            _logger.LogDebug("Check pq connection: Ok");
            return Task.FromResult(HealthCheckResult.Healthy());
        }
    }
}
