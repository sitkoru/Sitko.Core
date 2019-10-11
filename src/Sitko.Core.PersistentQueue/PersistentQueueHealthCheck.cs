using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NATS.Client;
using Sitko.Core.PersistentQueue.Common;

namespace Sitko.Core.PersistentQueue
{
    public class PersistentQueueHealthCheck : IHealthCheck
    {
        private readonly IPersistentQueueConnectionFactory _connectionFactory;
        private readonly ILogger<PersistentQueueHealthCheck> _logger;

        public PersistentQueueHealthCheck(IPersistentQueueConnectionFactory connectionFactory, ILogger<PersistentQueueHealthCheck> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var connections = _connectionFactory.GetConnections();
            foreach (var connection in connections)
            {
                if (connection.Connection.NATSConnection.State != ConnState.CONNECTED)
                {
                    _logger.LogError("Check pq connection: Fail {connectionId}. Last error: {errorText}", connection.Id,
                        connection.Connection.NATSConnection.LastError.ToString());
                    return Task.FromResult(HealthCheckResult.Unhealthy());
                }
            }

            _logger.LogDebug("Check pq connection: Ok");
            return Task.FromResult(HealthCheckResult.Healthy());
        }
    }
}
