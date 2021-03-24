using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Elastic.Apm.NetCoreAll;
using Elastic.Apm.SerilogEnricher;
using Elastic.CommonSchema.Serilog;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
using Sitko.Core.App;
using Sitko.Core.App.Logging;
using Sitko.Core.App.Web;

namespace Sitko.Core.ElasticStack
{
    public class ElasticStackModule : BaseApplicationModule<ElasticStackModuleConfig>, IWebApplicationModule
    {
        public ElasticStackModule(ElasticStackModuleConfig config, Application application) : base(config, application)
        {
        }

        public override void ConfigureLogging(LoggerConfiguration loggerConfiguration,
            LogLevelSwitcher logLevelSwitcher,
            IConfiguration configuration, IHostEnvironment environment)
        {
            base.ConfigureLogging(loggerConfiguration, logLevelSwitcher, configuration, environment);
            if (Config.LoggingEnabled)
            {
                var options = new ElasticsearchSinkOptions(Config.ElasticSearchUrls)
                {
                    CustomFormatter = new EcsTextFormatter(),
                    AutoRegisterTemplate = true,
                    IndexFormat =
                        Config.LoggingIndexFormat ??
                        $"dotnet-{Assembly.GetExecutingAssembly().GetName().Name!.ToLower().Replace(".", "-")}-{environment.EnvironmentName.ToLower().Replace(".", "-")}-{DateTime.UtcNow:yyyy-MM}",
                    LevelSwitch = logLevelSwitcher.Switch
                };
                if (Config.LoggingTemplateVersion is not null)
                {
                    options.AutoRegisterTemplateVersion = Config.LoggingTemplateVersion.Value;
                    options.NumberOfReplicas = Config.LoggingNumberOfReplicas;
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

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            if (Config != null && Config.ApmEnabled)
            {
                configuration["ElasticApm:ServiceName"] = Application.Name;
                configuration["ElasticApm:ServiceVersion"] = Application.Version;
                configuration["ElasticApm:TransactionSampleRate"] =
                    Config.ApmTransactionSampleRate.ToString(CultureInfo.InvariantCulture);
                configuration["ElasticApm:TransactionMaxSpans"] = Config.ApmTransactionMaxSpans.ToString();
                configuration["ElasticApm:CentralConfig"] = Config.ApmCentralConfig.ToString();
                configuration["ElasticApm:SanitizeFieldNames"] = Config.ApmCentralConfig.ToString();
                if (Config.ApmSanitizeFieldNames != null && Config.ApmSanitizeFieldNames.Any())
                {
                    configuration["ElasticApm:SanitizeFieldNames"] = string.Join(", ", Config.ApmSanitizeFieldNames);
                }

                if (Config.ApmGlobalLabels.Any())
                {
                    configuration["ElasticApm:GlobalLabels"] =
                        string.Join(",", Config.ApmGlobalLabels.Select(kv => $"{kv.Key}={kv.Value}"));
                }

                if (Config.ApmServerUrls != null && Config.ApmServerUrls.Any())
                {
                    configuration["ElasticApm:ServerUrls"] = string.Join(",", Config.ApmServerUrls);
                }

                configuration["ElasticApm:SecretToken"] = Config.ApmSecretToken;
                configuration["ElasticApm:ApiKey"] = Config.ApmApiKey;
                configuration["ElasticApm:VerifyServerCert"] = Config.ApmVerifyServerCert.ToString();
                configuration["ElasticApm:FlushInterval"] = $"{Config.ApmFlushInterval.TotalSeconds}s";
                configuration["ElasticApm:MaxBatchEventCount"] = Config.ApmMaxBatchEventCount.ToString();
                configuration["ElasticApm:MaxQueueEventCount"] = Config.ApmMaxQueueEventCount.ToString();
                configuration["ElasticApm:MetricsInterval"] = $"{Config.ApmMetricsInterval.TotalSeconds}s";
                if (Config.ApmDisableMetrics != null && Config.ApmDisableMetrics.Any())
                {
                    configuration["ElasticApm:DisableMetrics"] = string.Join(",", Config.ApmDisableMetrics);
                }

                configuration["ElasticApm:CaptureBody"] = Config.ApmCaptureBody;
                if (Config.ApmCaptureBodyContentTypes != null && Config.ApmCaptureBodyContentTypes.Any())
                {
                    configuration["ElasticApm:CaptureBodyContentTypes"] =
                        string.Join(",", Config.ApmCaptureBodyContentTypes);
                }

                configuration["ElasticApm:CaptureHeaders"] = Config.ApmCaptureHeaders.ToString();
                configuration["ElasticApm:UseElasticTraceparentHeader"] =
                    Config.ApmUseElasticTraceparentHeader.ToString();
                configuration["ElasticApm:StackTraceLimit"] = Config.ApmStackTraceLimit.ToString();
                configuration["ElasticApm:SpanFramesMinDuration"] =
                    $"{Config.ApmSpanFramesMinDuration.TotalMilliseconds}ms";
                configuration["ElasticApm:ApmLogLevel"] = Config.ApmLogLevel;
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

        public void ConfigureAppBuilder(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
            if (Config.ApmEnabled)
            {
                appBuilder.UseAllElasticApm(configuration);
            }
        }
    }
}
