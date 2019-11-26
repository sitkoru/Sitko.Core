using System;
using System.Collections.Generic;
using Google.Protobuf;
using Sitko.Core.Metrics;

namespace Sitko.Core.PersistentQueue
{
    public class PersistentQueueMetricsCollector
    {
        private readonly IMetricsCollector _metricsCollector;
        private const string Context = "pq";

        public PersistentQueueMetricsCollector(IMetricsCollector metricsCollector)
        {
            _metricsCollector = metricsCollector;
        }

        public void TrackSend(IMessage message)
        {
            _metricsCollector.Meter("messages_sent", 1,
                new Dictionary<string, string> {{"MessageType", message.GetType().FullName}}, Context);
        }

        public void TrackProduce(IMessage message, long latency, bool success)
        {            
        }

        public void TrackReceive(IMessage message, PersistentQueueMessageContext context)
        {
            var latency = (DateTimeOffset.Now - context.RootMessageDate)?.TotalMilliseconds;
            _metricsCollector.Meter("messages_received", 1,
                new Dictionary<string, string> {{"MessageType", message.GetType().FullName}}, Context);
            if (latency != null)
            {
                _metricsCollector.Histogram("messages_latency", (long) latency,
                    new Dictionary<string, string> {{"MessageType", message.GetType().FullName}}, Context);
            }
        }

        public void TrackProcess(IMessage message, long milliseconds)
        {
            _metricsCollector.Histogram("messages_process", milliseconds,
                new Dictionary<string, string> {{"MessageType", message.GetType().FullName}}, Context);
        }


        public void TrackSize(IMessage message, long bytes)
        {
            _metricsCollector.Histogram("messages_size", bytes,
                new Dictionary<string, string> {{"MessageType", message.GetType().FullName}}, Context);
        }

        public void TrackPoolSize(int connectionsCount)
        {
            _metricsCollector.Histogram("connection_poll_size", connectionsCount, context: Context);
        }

        public void TrackConnectionUsageTime(long totalMilliseconds)
        {
            _metricsCollector.Histogram("connection_usage_time", totalMilliseconds, context: Context);
        }

        public void TrackConnectionPrune()
        {
            _metricsCollector.Meter("connection_prune", context: Context);
        }
    }
}
