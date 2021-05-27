using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Elastic.Apm.NetCoreAll;
using Elastic.Apm.SerilogEnricher;
using Elastic.CommonSchema.Serilog;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
using Sitko.Core.App;
using Sitko.Core.App.Logging;

namespace Sitko.Core.ElasticStack
{
    public class ElasticStackModule : BaseApplicationModule<ElasticStackModuleConfig>,
        IHostBuilderModule<ElasticStackModuleConfig>
    {
        public void ConfigureHostBuilder(ApplicationContext context, IHostBuilder hostBuilder,
            ElasticStackModuleConfig config)
        {
            if (config.ApmEnabled)
            {
                Environment.SetEnvironmentVariable("ELASTIC_APM_SERVICE_NAME", context.Name);
                Environment.SetEnvironmentVariable("ELASTIC_APM_SERVICE_VERSION", context.Version);
                Environment.SetEnvironmentVariable("ELASTIC_APM_TRANSACTION_SAMPLE_RATE",
                    config.ApmTransactionSampleRate.ToString(CultureInfo.InvariantCulture));
                Environment.SetEnvironmentVariable("ElasticApm:TransactionMaxSpans",
                    config.ApmTransactionMaxSpans.ToString());
                Environment.SetEnvironmentVariable("ElasticApm:CentralConfig", config.ApmCentralConfig.ToString());
                Environment.SetEnvironmentVariable("ElasticApm:SanitizeFieldNames", config.ApmCentralConfig.ToString());
                if (config.ApmSanitizeFieldNames != null && config.ApmSanitizeFieldNames.Any())
                {
                    Environment.SetEnvironmentVariable("ElasticApm:SanitizeFieldNames",
                        string.Join(", ", config.ApmSanitizeFieldNames));
                }

                if (config.ApmGlobalLabels.Any())
                {
                    Environment.SetEnvironmentVariable("ElasticApm:GlobalLabels",
                        string.Join(",", config.ApmGlobalLabels.Select(kv => $"{kv.Key}={kv.Value}")));
                }

                if (config.ApmServerUrls != null && config.ApmServerUrls.Any())
                {
                    Environment.SetEnvironmentVariable("ElasticApm:ServerUrls", string.Join(",", config.ApmServerUrls));
                }

                Environment.SetEnvironmentVariable("ElasticApm:SecretToken", config.ApmSecretToken);
                Environment.SetEnvironmentVariable("ElasticApm:ApiKey", config.ApmApiKey);
                Environment.SetEnvironmentVariable("ElasticApm:VerifyServerCert",
                    config.ApmVerifyServerCert.ToString());
                Environment.SetEnvironmentVariable("ElasticApm:FlushInterval",
                    $"{config.ApmFlushInterval.TotalSeconds}s");
                Environment.SetEnvironmentVariable("ElasticApm:MaxBatchEventCount",
                    config.ApmMaxBatchEventCount.ToString());
                Environment.SetEnvironmentVariable("ElasticApm:MaxQueueEventCount",
                    config.ApmMaxQueueEventCount.ToString());
                Environment.SetEnvironmentVariable("ElasticApm:MetricsInterval",
                    $"{config.ApmMetricsInterval.TotalSeconds}s");
                if (config.ApmDisableMetrics != null && config.ApmDisableMetrics.Any())
                {
                    Environment.SetEnvironmentVariable("ElasticApm:DisableMetrics",
                        string.Join(",", config.ApmDisableMetrics));
                }

                Environment.SetEnvironmentVariable("ElasticApm:CaptureBody", config.ApmCaptureBody);
                if (config.ApmCaptureBodyContentTypes != null && config.ApmCaptureBodyContentTypes.Any())
                {
                    Environment.SetEnvironmentVariable("ElasticApm:CaptureBodyContentTypes",
                        string.Join(",", config.ApmCaptureBodyContentTypes));
                }

                Environment.SetEnvironmentVariable("ElasticApm:CaptureHeaders", config.ApmCaptureHeaders.ToString());
                Environment.SetEnvironmentVariable("ElasticApm:UseElasticTraceparentHeader",
                    config.ApmUseElasticTraceparentHeader.ToString());
                Environment.SetEnvironmentVariable("ElasticApm:StackTraceLimit", config.ApmStackTraceLimit.ToString());
                Environment.SetEnvironmentVariable("ElasticApm:SpanFramesMinDuration",
                    $"{config.ApmSpanFramesMinDuration.TotalMilliseconds}ms");
                Environment.SetEnvironmentVariable("ElasticApm:ApmLogLevel", config.ApmLogLevel);
                hostBuilder.UseAllElasticApm();
            }
        }

        public override void ConfigureLogging(ApplicationContext context, ElasticStackModuleConfig config,
            LoggerConfiguration loggerConfiguration,
            LogLevelSwitcher logLevelSwitcher)
        {
            base.ConfigureLogging(context, config, loggerConfiguration, logLevelSwitcher);
            if (config.LoggingEnabled)
            {
                var rolloverAlias = string.IsNullOrEmpty(config.LoggingLiferRolloverAlias)
                    ? $"dotnet-logs-{context.Name.ToLower().Replace(".", "-")}-{context.Environment.EnvironmentName.ToLower().Replace(".", "-")}"
                    : config.LoggingLiferRolloverAlias;
                var options = new ElasticsearchSinkOptions(config.ElasticSearchUrls)
                {
                    CustomFormatter = new EcsTextFormatter(),
                    AutoRegisterTemplate = true,
                    AutoRegisterTemplateVersion = config.LoggingTemplateVersion ?? AutoRegisterTemplateVersion.ESv7,
                    NumberOfReplicas = config.LoggingNumberOfReplicas,
                    IndexFormat =
                        config.LoggingIndexFormat ??
                        $"dotnet-logs-{context.Name.ToLower().Replace(".", "-")}-{context.Name.ToLower().Replace(".", "-")}-{DateTime.UtcNow:yyyy-MM}",
                    LevelSwitch = logLevelSwitcher.Switch,
                    TemplateName = rolloverAlias
                };

                if (!string.IsNullOrEmpty(config.LoggingLifeCycleName))
                {
                    options.TemplateCustomSettings = new Dictionary<string, string>
                    {
                        {"lifecycle.name", config.LoggingLifeCycleName}, {"lifecycle.rollover_alias", rolloverAlias}
                    };
                    options.IndexAliases = new[] {rolloverAlias};
                }

                loggerConfiguration.Enrich.WithElasticApmCorrelationInfo()
                    .WriteTo.Elasticsearch(options)
                    .Enrich.WithProperty("ApplicationName", context.Name)
                    .Enrich.WithProperty("ApplicationVersion", context.Version);
            }

            if (config.ApmEnabled)
            {
                loggerConfiguration.MinimumLevel.Override("Elastic.Apm", LogEventLevel.Error);
            }
        }

        public override string GetConfigKey()
        {
            return "ElasticApm";
        }
    }
}
