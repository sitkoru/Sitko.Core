using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Elastic.Apm.NetCoreAll;
using Elastic.Apm.SerilogEnricher;
using Elastic.CommonSchema.Serilog;
using Microsoft.Extensions.Configuration;
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
        public ElasticStackModule(ElasticStackModuleConfig config, Application application) : base(config, application)
        {
        }

        public void ConfigureHostBuilder(IHostBuilder hostBuilder, IConfiguration configuration,
            IHostEnvironment environment)
        {
            if (Config.ApmEnabled)
            {
                Environment.SetEnvironmentVariable("ELASTIC_APM_SERVICE_NAME", Application.Name);
                Environment.SetEnvironmentVariable("ELASTIC_APM_SERVICE_VERSION", Application.Version);
                Environment.SetEnvironmentVariable("ELASTIC_APM_TRANSACTION_SAMPLE_RATE",
                    Config.ApmTransactionSampleRate.ToString(CultureInfo.InvariantCulture));
                Environment.SetEnvironmentVariable("ElasticApm:TransactionMaxSpans",
                    Config.ApmTransactionMaxSpans.ToString());
                Environment.SetEnvironmentVariable("ElasticApm:CentralConfig", Config.ApmCentralConfig.ToString());
                Environment.SetEnvironmentVariable("ElasticApm:SanitizeFieldNames", Config.ApmCentralConfig.ToString());
                if (Config.ApmSanitizeFieldNames != null && Config.ApmSanitizeFieldNames.Any())
                {
                    Environment.SetEnvironmentVariable("ElasticApm:SanitizeFieldNames",
                        string.Join(", ", Config.ApmSanitizeFieldNames));
                }

                if (Config.ApmGlobalLabels.Any())
                {
                    Environment.SetEnvironmentVariable("ElasticApm:GlobalLabels",
                        string.Join(",", Config.ApmGlobalLabels.Select(kv => $"{kv.Key}={kv.Value}")));
                }

                if (Config.ApmServerUrls != null && Config.ApmServerUrls.Any())
                {
                    Environment.SetEnvironmentVariable("ElasticApm:ServerUrls", string.Join(",", Config.ApmServerUrls));
                }

                Environment.SetEnvironmentVariable("ElasticApm:SecretToken", Config.ApmSecretToken);
                Environment.SetEnvironmentVariable("ElasticApm:ApiKey", Config.ApmApiKey);
                Environment.SetEnvironmentVariable("ElasticApm:VerifyServerCert",
                    Config.ApmVerifyServerCert.ToString());
                Environment.SetEnvironmentVariable("ElasticApm:FlushInterval",
                    $"{Config.ApmFlushInterval.TotalSeconds}s");
                Environment.SetEnvironmentVariable("ElasticApm:MaxBatchEventCount",
                    Config.ApmMaxBatchEventCount.ToString());
                Environment.SetEnvironmentVariable("ElasticApm:MaxQueueEventCount",
                    Config.ApmMaxQueueEventCount.ToString());
                Environment.SetEnvironmentVariable("ElasticApm:MetricsInterval",
                    $"{Config.ApmMetricsInterval.TotalSeconds}s");
                if (Config.ApmDisableMetrics != null && Config.ApmDisableMetrics.Any())
                {
                    Environment.SetEnvironmentVariable("ElasticApm:DisableMetrics",
                        string.Join(",", Config.ApmDisableMetrics));
                }

                Environment.SetEnvironmentVariable("ElasticApm:CaptureBody", Config.ApmCaptureBody);
                if (Config.ApmCaptureBodyContentTypes != null && Config.ApmCaptureBodyContentTypes.Any())
                {
                    Environment.SetEnvironmentVariable("ElasticApm:CaptureBodyContentTypes",
                        string.Join(",", Config.ApmCaptureBodyContentTypes));
                }

                Environment.SetEnvironmentVariable("ElasticApm:CaptureHeaders", Config.ApmCaptureHeaders.ToString());
                Environment.SetEnvironmentVariable("ElasticApm:UseElasticTraceparentHeader",
                    Config.ApmUseElasticTraceparentHeader.ToString());
                Environment.SetEnvironmentVariable("ElasticApm:StackTraceLimit", Config.ApmStackTraceLimit.ToString());
                Environment.SetEnvironmentVariable("ElasticApm:SpanFramesMinDuration",
                    $"{Config.ApmSpanFramesMinDuration.TotalMilliseconds}ms");
                Environment.SetEnvironmentVariable("ElasticApm:ApmLogLevel", Config.ApmLogLevel);
                hostBuilder.UseAllElasticApm();
            }
        }

        public override void ConfigureLogging(LoggerConfiguration loggerConfiguration,
            LogLevelSwitcher logLevelSwitcher,
            IConfiguration configuration, IHostEnvironment environment)
        {
            base.ConfigureLogging(loggerConfiguration, logLevelSwitcher, configuration, environment);
            if (Config.LoggingEnabled)
            {
                var rolloverAlias = string.IsNullOrEmpty(Config.LoggingLiferRolloverAlias)
                    ? $"dotnet-logs-{environment.ApplicationName.ToLower().Replace(".", "-")}-{environment.EnvironmentName.ToLower().Replace(".", "-")}"
                    : Config.LoggingLiferRolloverAlias;
                var options = new ElasticsearchSinkOptions(Config.ElasticSearchUrls)
                {
                    CustomFormatter = new EcsTextFormatter(),
                    AutoRegisterTemplate = true,
                    AutoRegisterTemplateVersion = Config.LoggingTemplateVersion ?? AutoRegisterTemplateVersion.ESv7,
                    NumberOfReplicas = Config.LoggingNumberOfReplicas,
                    IndexFormat =
                        Config.LoggingIndexFormat ??
                        $"dotnet-logs-{environment.ApplicationName.ToLower().Replace(".", "-")}-{environment.EnvironmentName.ToLower().Replace(".", "-")}-{DateTime.UtcNow:yyyy-MM}",
                    LevelSwitch = logLevelSwitcher.Switch,
                    TemplateName = rolloverAlias
                };

                if (!string.IsNullOrEmpty(Config.LoggingLifeCycleName))
                {
                    options.TemplateCustomSettings = new Dictionary<string, string>
                    {
                        {"lifecycle.name", Config.LoggingLifeCycleName}, {"lifecycle.rollover_alias", rolloverAlias}
                    };
                    options.IndexAliases = new[] {rolloverAlias};
                }

                loggerConfiguration.Enrich.WithElasticApmCorrelationInfo()
                    .WriteTo.Elasticsearch(options)
                    .Enrich.WithProperty("ApplicationName", Application.Name)
                    .Enrich.WithProperty("ApplicationVersion", Application.Version);
            }

            if (Config.ApmEnabled)
            {
                loggerConfiguration.MinimumLevel.Override("Elastic.Apm", LogEventLevel.Error);
            }
        }

        public override void CheckConfig()
        {
            base.CheckConfig();
            if (Config.ApmEnabled)
            {
                if (Config.ApmServerUrls == null || !Config.ApmServerUrls.Any())
                {
                    throw new ArgumentException("ApmServerUrls can't be empty");
                }
            }

            if (Config.LoggingEnabled)
            {
                if (Config.ElasticSearchUrls == null || !Config.ElasticSearchUrls.Any())
                {
                    throw new ArgumentException("ElasticSearchUrls can't be empty");
                }
            }
        }
    }
}
