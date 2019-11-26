using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NATS.Client;

namespace Sitko.Core.PersistentQueue.Nats
{
    public class PersistentQueueHealthCheck : IHealthCheck
    {
        private readonly NatsConnectionFactory _connectionFactory;

        private readonly ILogger<PersistentQueueHealthCheck> _logger;

        public PersistentQueueHealthCheck(
            NatsConnectionFactory connectionFactory,
            ILogger<PersistentQueueHealthCheck> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var connection = _connectionFactory.GetCurrentConnection();
            if (connection != null && connection.StanConnection.NATSConnection.State != ConnState.CONNECTED)
            {
                _logger.LogError("Check pq connection: Fail {connectionId}. Last error: {errorText}", connection.Id,
                    connection.StanConnection.NATSConnection.LastError.ToString());
                return Task.FromResult(HealthCheckResult.Unhealthy());
            }

            _logger.LogDebug("Check pq connection: Ok");
            return Task.FromResult(HealthCheckResult.Healthy());
        }
    }
}
