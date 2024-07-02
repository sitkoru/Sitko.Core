using FluentValidation;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
using Sitko.Core.App;

namespace Sitko.Core.ElasticStack;

public class ElasticStackModuleOptions : BaseModuleOptions
{
    public bool LoggingEnabled => ElasticSearchUrls.Any();
    public bool ApmEnabled => ApmServerUrls.Any();
    public List<Uri> ElasticSearchUrls { get; set; } = new();
    public double ApmTransactionSampleRate { get; set; } = 1.0;
    public int ApmTransactionMaxSpans { get; set; } = 500;
    public bool ApmCentralConfig { get; set; } = true;
    public List<string>? ApmSanitizeFieldNames { get; set; }
    public Dictionary<string, string> ApmGlobalLabels { get; set; } = new();
    public List<Uri> ApmServerUrls { get; set; } = new();
    public string? ApmSecretToken { get; set; }
    public string? ApmApiKey { get; set; }
    public bool ApmVerifyServerCert { get; set; } = true;
    public int ApmFlushIntervalInSeconds { get; set; } = 10;
    public int ApmMaxBatchEventCount { get; set; } = 10;
    public int ApmMaxQueueEventCount { get; set; } = 1000;
    public int ApmMetricsIntervalInSeconds { get; set; } = 30;
    public List<string>? ApmDisableMetrics { get; set; }
    public string ApmCaptureBody { get; set; } = "off";
    public List<string>? ApmCaptureBodyContentTypes { get; set; }
    public bool ApmCaptureHeaders { get; set; } = true;
    public bool ApmUseElasticTraceparentHeader { get; set; } = true;
    public int ApmStackTraceLimit { get; set; } = 50;
    public double ApmSpanFramesMinDurationInSeconds { get; set; } = 0.5;
    public string ApmLogLevel { get; set; } = "Error";
    public string? LoggingIndexFormat { get; set; }
    public AutoRegisterTemplateVersion? LoggingTemplateVersion { get; set; }
    public int? LoggingNumberOfReplicas { get; set; }
    public string? LoggingLifeCycleName { get; set; }
    public string? LoggingRolloverAlias { get; set; }
    public EmitEventFailureHandling EmitEventFailure { get; set; } = EmitEventFailureHandling.WriteToSelfLog;
    public ILogEventSink? FailureSink { get; set; }
    public Action<LogEvent, Exception>? FailureCallback { get; set; }
    public string? LogIndexTypeName { get; set; }
}

public class ElasticStackModuleOptionsValidator : AbstractValidator<ElasticStackModuleOptions>
{
    public ElasticStackModuleOptionsValidator()
    {
        RuleFor(o => o.ApmServerUrls).NotEmpty().When(o => o.ApmEnabled)
            .WithMessage("ApmServerUrls can't be empty");
        RuleFor(o => o.ElasticSearchUrls).NotEmpty().When(o => o.LoggingEnabled)
            .WithMessage("ElasticSearchUrls can't be empty");
    }
}

