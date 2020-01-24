using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Sitko.Core.Metrics;

namespace Sitko.Core.Queue.Middleware
{
    public class MetricsMiddleware : IQueueMiddleware
    {
        private readonly IMetricsCollector _metricsCollector;
        private const string Context = "queue";
        private readonly ConcurrentDictionary<Guid, Stopwatch> _timers = new ConcurrentDictionary<Guid, Stopwatch>();

        public long SentCount { get; private set; }
        public long ReceivedCount { get; private set; }
        private double Latency;
        private long ProcessTime;
        public double AvgLatency => ReceivedCount > 0 ? Latency / ReceivedCount : 0;
        public double AvgProcessTime => ReceivedCount > 0 ? ProcessTime / ReceivedCount : 0;

        public MetricsMiddleware(IMetricsCollector metricsCollector)
        {
            _metricsCollector = metricsCollector;
        }

        public Task OnAfterPublishAsync(object message, QueueMessageContext messageContext)
        {
            _metricsCollector.Meter("messages_sent", 1,
                new Dictionary<string, string> {{"MessageType", message.GetType().FullName}}, Context);
            SentCount++;
            return Task.FromResult(new QueuePublishResult());
        }

        public Task<bool> OnBeforeReceiveAsync(object message, QueueMessageContext messageContext)
        {
            var sw = new Stopwatch();
            sw.Start();
            _timers.TryAdd(messageContext.Id, sw);
            return Task.FromResult(true);
        }

        public Task OnAfterReceiveAsync(object message, QueueMessageContext messageContext)
        {
            ReceivedCount++;
            _metricsCollector.Meter("messages_received", 1,
                new Dictionary<string, string> {{"MessageType", message.GetType().FullName}}, Context);

            var latency = (DateTimeOffset.Now - messageContext.RootMessageDate)?.TotalMilliseconds;
            if (latency != null)
            {
                Latency += latency.Value;
                _metricsCollector.Histogram("messages_latency", (long)latency,
                    new Dictionary<string, string> {{"MessageType", message.GetType().FullName}}, Context);
            }

            if (_timers.TryRemove(messageContext.Id, out var timer))
            {
                timer.Stop();
                _metricsCollector.Histogram("messages_process", timer.ElapsedMilliseconds,
                    new Dictionary<string, string> {{"MessageType", message.GetType().FullName}}, Context);
                ProcessTime += timer.ElapsedMilliseconds;
            }

            return Task.FromResult(true);
        }
    }
}
