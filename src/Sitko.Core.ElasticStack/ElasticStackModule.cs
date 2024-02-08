using System.Globalization;
using Elastic.Apm.Config;
using Elastic.Apm.NetCoreAll;
using Elastic.Apm.SerilogEnricher;
using Elastic.CommonSchema.Serilog;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
using Sitko.Core.App;

namespace Sitko.Core.ElasticStack;

public class ElasticStackModule : BaseApplicationModule<ElasticStackModuleOptions>,
    IHostBuilderModule<ElasticStackModuleOptions>, ILoggingModule<ElasticStackModuleOptions>,
    IConfigurationModule<ElasticStackModuleOptions>
{
    public override string OptionsKey => "ElasticApm";

    public void PostConfigureHostBuilder(IApplicationContext context, IHostApplicationBuilder hostBuilder,
        ElasticStackModuleOptions startupOptions)
    {
        if (startupOptions.ApmEnabled)
        {
            hostBuilder.ToHostBuilder().UseAllElasticApm();
        }
    }

    public LoggerConfiguration ConfigureLogging(IApplicationContext context, ElasticStackModuleOptions options,
        LoggerConfiguration loggerConfiguration)
    {
        if (options.LoggingEnabled)
        {
            var sinkOptions = new ElasticsearchSinkOptions(options.ElasticSearchUrls)
            {
                CustomFormatter = new EcsTextFormatter(),
                AutoRegisterTemplate = true,
                AutoRegisterTemplateVersion = options.LoggingTemplateVersion ?? AutoRegisterTemplateVersion.ESv7,
                NumberOfReplicas = options.LoggingNumberOfReplicas,
                IndexFormat =
                    options.LoggingIndexFormat ??
                    $"dotnet-logs-{context.Name.ToLower(CultureInfo.InvariantCulture).Replace(".", "-")}-{context.Name.ToLower(CultureInfo.InvariantCulture).Replace(".", "-")}-{DateTime.UtcNow:yyyy-MM}",
                EmitEventFailure = options.EmitEventFailure,
                FailureCallback = options.FailureCallback,
                FailureSink = options.FailureSink,
                TypeName = options.LogIndexTypeName
            };
            if (!string.IsNullOrEmpty(options.LoggingLifeCycleName))
            {
                sinkOptions.TemplateCustomSettings = new Dictionary<string, string>
                {
                    { "lifecycle.name", options.LoggingLifeCycleName }
                };
            }

            if (!string.IsNullOrEmpty(options.LoggingRolloverAlias))
            {
                sinkOptions.TemplateName = options.LoggingRolloverAlias;
                sinkOptions.IndexAliases = new[] { options.LoggingRolloverAlias };
                sinkOptions.TemplateCustomSettings["lifecycle.rollover_alias"] = options.LoggingRolloverAlias;
            }

            loggerConfiguration = loggerConfiguration
                .Enrich.WithElasticApmCorrelationInfo()
                .WriteTo.Elasticsearch(sinkOptions)
                // meta for EcsTextFormatter
                .Enrich.WithProperty("ApplicationId", context.Id)
                .Enrich.WithProperty("ApplicationName", context.Name)
                .Enrich.WithProperty("ApplicationVersion", context.Version);
        }

        if (options.ApmEnabled)
        {
            loggerConfiguration = loggerConfiguration.MinimumLevel.Override("Elastic.Apm", LogEventLevel.Error);
        }

        return loggerConfiguration;
    }

    public void ConfigureAppConfiguration(IApplicationContext context,
        IConfigurationBuilder configurationBuilder,
        ElasticStackModuleOptions startupOptions)
    {
        if (startupOptions.ApmEnabled)
        {
            var apmOptions = new Dictionary<string, string?>();
            AddOption(apmOptions, ConfigurationOption.ServiceName, context.Name);
            AddOption(apmOptions, ConfigurationOption.ServiceVersion, context.Version);
            AddOption(apmOptions, ConfigurationOption.TransactionSampleRate,
                startupOptions.ApmTransactionSampleRate.ToString(CultureInfo.InvariantCulture));
            AddOption(apmOptions, ConfigurationOption.TransactionMaxSpans,
                startupOptions.ApmTransactionMaxSpans.ToString(CultureInfo.InvariantCulture));
            AddOption(apmOptions, ConfigurationOption.CentralConfig,
                startupOptions.ApmCentralConfig.ToString(CultureInfo.InvariantCulture));
            AddOption(apmOptions, ConfigurationOption.SecretToken, startupOptions.ApmSecretToken);
            AddOption(apmOptions, ConfigurationOption.ApiKey, startupOptions.ApmApiKey);
            AddOption(apmOptions, ConfigurationOption.VerifyServerCert, startupOptions.ApmVerifyServerCert.ToString());
            AddOption(apmOptions, ConfigurationOption.FlushInterval,
                $"{TimeSpan.FromSeconds(startupOptions.ApmFlushIntervalInSeconds).TotalSeconds}s");
            AddOption(apmOptions, ConfigurationOption.MaxBatchEventCount,
                startupOptions.ApmMaxBatchEventCount.ToString(CultureInfo.InvariantCulture));
            AddOption(apmOptions, ConfigurationOption.MaxQueueEventCount,
                startupOptions.ApmMaxQueueEventCount.ToString(CultureInfo.InvariantCulture));
            AddOption(apmOptions, ConfigurationOption.MetricsInterval,
                $"{TimeSpan.FromSeconds(startupOptions.ApmMetricsIntervalInSeconds).TotalSeconds}s");
            AddOption(apmOptions, ConfigurationOption.CaptureBody, startupOptions.ApmCaptureBody);
            AddOption(apmOptions, ConfigurationOption.CaptureHeaders,
                startupOptions.ApmCaptureHeaders.ToString(CultureInfo.InvariantCulture));
            AddOption(apmOptions, ConfigurationOption.UseElasticTraceparentHeader,
                startupOptions.ApmUseElasticTraceparentHeader.ToString(CultureInfo.InvariantCulture));
            AddOption(apmOptions, ConfigurationOption.StackTraceLimit,
                startupOptions.ApmStackTraceLimit.ToString(CultureInfo.InvariantCulture));
            AddOption(apmOptions, ConfigurationOption.SpanStackTraceMinDuration,
                $"{TimeSpan.FromSeconds(startupOptions.ApmSpanFramesMinDurationInSeconds).TotalMilliseconds}ms");
            AddOption(apmOptions, ConfigurationOption.LogLevel, startupOptions.ApmLogLevel);
            AddOption(apmOptions, ConfigurationOption.ServerUrl,
                startupOptions.ApmServerUrls.OrderBy(_ => Guid.NewGuid()).First().ToString());

            if (startupOptions.ApmSanitizeFieldNames != null && startupOptions.ApmSanitizeFieldNames.Count != 0)
            {
                AddOption(apmOptions, ConfigurationOption.SanitizeFieldNames,
                    string.Join(", ", startupOptions.ApmSanitizeFieldNames));
            }

            if (startupOptions.ApmGlobalLabels.Count != 0)
            {
                AddOption(apmOptions, ConfigurationOption.GlobalLabels,
                    string.Join(",", startupOptions.ApmGlobalLabels.Select(kv => $"{kv.Key}={kv.Value}")));
            }

            if (startupOptions.ApmDisableMetrics != null && startupOptions.ApmDisableMetrics.Count != 0)
            {
                AddOption(apmOptions, ConfigurationOption.DisableMetrics,
                    string.Join(",", startupOptions.ApmDisableMetrics));
            }

            if (startupOptions.ApmCaptureBodyContentTypes != null &&
                startupOptions.ApmCaptureBodyContentTypes.Count != 0)
            {
                AddOption(apmOptions, ConfigurationOption.CaptureBodyContentTypes,
                    string.Join(",", startupOptions.ApmCaptureBodyContentTypes));
            }

            configurationBuilder.AddInMemoryCollection(apmOptions);
        }
    }

    private static void AddOption(IDictionary<string, string?> options, ConfigurationOption option, string? value) =>
        options[$"ElasticApm:{Enum.GetName(typeof(ConfigurationOption), option)}"] = value;

    public void CheckConfiguration(IApplicationContext context, IServiceProvider serviceProvider)
    {
        // do nothing
    }
}
