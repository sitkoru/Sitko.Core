using System;
using System.Collections.Generic;
using System.Linq;
using Serilog.Sinks.Elasticsearch;

namespace Sitko.Core.ElasticStack
{
    public class ElasticStackModuleConfig
    {
        public bool LoggingEnabled { get; private set; }
        public bool ApmEnabled { get; private set; }
        public List<Uri>? ElasticSearchUrls { get; protected set; }
        public double ApmTransactionSampleRate { get; set; } = 1.0;
        public int ApmTransactionMaxSpans { get; set; } = 500;
        public bool ApmCentralConfig { get; set; } = true;
        public List<string>? ApmSanitizeFieldNames { get; set; }
        public readonly Dictionary<string, string> ApmGlobalLabels = new Dictionary<string, string>();
        public List<Uri>? ApmServerUrls { get; protected set; }
        public string? ApmSecretToken { get; set; }
        public string? ApmApiKey { get; set; }
        public bool ApmVerifyServerCert { get; set; } = true;
        public TimeSpan ApmFlushInterval { get; set; } = TimeSpan.FromSeconds(10);
        public int ApmMaxBatchEventCount { get; set; } = 10;
        public int ApmMaxQueueEventCount { get; set; } = 1000;
        public TimeSpan ApmMetricsInterval { get; set; } = TimeSpan.FromSeconds(30);
        public List<string>? ApmDisableMetrics { get; set; }
        public string ApmCaptureBody { get; set; } = "off";
        public List<string>? ApmCaptureBodyContentTypes { get; set; }
        public bool ApmCaptureHeaders { get; set; } = true;
        public bool ApmUseElasticTraceparentHeader { get; set; } = true;
        public int ApmStackTraceLimit { get; set; } = 50;
        public TimeSpan ApmSpanFramesMinDuration { get; set; } = TimeSpan.FromSeconds(0.5);
        public string ApmLogLevel { get; set; } = "Error";
        public string? LoggingIndexFormat { get; set; }
        public AutoRegisterTemplateVersion? LoggingTemplateVersion { get; set; }
        public int? LoggingNumberOfReplicas { get; set; }

        public ElasticStackModuleConfig EnableLogging(Uri elasticSearchUri)
        {
            return EnableLogging(new[] {elasticSearchUri});
        }

        public ElasticStackModuleConfig EnableLogging(IEnumerable<Uri> elasticSearchUrls)
        {
            LoggingEnabled = true;
            ElasticSearchUrls = elasticSearchUrls.ToList();
            return this;
        }

        public ElasticStackModuleConfig EnableApm(Uri apmUri)
        {
            return EnableApm(new[] {apmUri});
        }

        public ElasticStackModuleConfig EnableApm(IEnumerable<Uri> apmUrls)
        {
            ApmEnabled = true;
            ApmServerUrls = apmUrls.ToList();
            return this;
        }
    }
}
