using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Grpc.Core;
using Sitko.Core.Metrics;

namespace Sitko.Core.Grpc
{
    public sealed class GrpcMetricsCollector
    {
        private readonly IMetricsCollector _metricsCollector;
        private readonly string _serviceName;
        private readonly string _methodName;
        private readonly CollectorMode _mode;
        private readonly Stopwatch _stopwatch;
        private StatusCode _statusCode = StatusCode.OK;

        private GrpcMetricsCollector(IMetricsCollector metricsCollector, string serviceName,
            string methodName, CollectorMode mode)
        {
            _metricsCollector = metricsCollector;
            _serviceName = serviceName;
            _methodName = methodName;
            _mode = mode;
            _stopwatch = Stopwatch.StartNew();
        }

        public static GrpcMetricsCollector Begin(IMetricsCollector metricsCollector, string serviceName,
            string methodName, CollectorMode mode)
        {
            return new GrpcMetricsCollector(metricsCollector, serviceName, methodName, mode);
        }

        public void SetStatusCode(StatusCode statusCode)
        {
            _statusCode = statusCode;
        }

        public void End()
        {
            _stopwatch.Stop();
            if (_mode == CollectorMode.Server)
            {
                CollectCount();
                CollectLatency();
            }
        }
   

        private void CollectLatency()
        {
            _metricsCollector?.Histogram("method_call", _stopwatch.ElapsedMilliseconds, GetTags(), "grpc");
        }

        private void CollectCount()
        {
            _metricsCollector.Meter("method_call_count", 1, GetTags(), "grpc");
        }

        private Dictionary<string, string> GetTags()
        {
            return new Dictionary<string, string>
            {
                {"ServiceName", _serviceName},
                {"MethodName", _methodName.Split('/').Last()},
                {"StatusCode", _statusCode.ToString()}
            };
        }
    }

    public enum CollectorMode
    {
        Server,
        Client
    }
}
