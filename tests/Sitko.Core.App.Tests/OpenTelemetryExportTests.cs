using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using Google.Protobuf;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.App.OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Xunit;

namespace Sitko.Core.App.Tests
{
    [CollectionDefinition("OTLP process environment", DisableParallelization = true)]
    public class OpenTelemetryEnvironmentCollection;

    [Collection("OTLP process environment")]
    public class OpenTelemetryExportTests
    {
        private const string Marker = "sitko-otel-acceptance";
        private static readonly string[] Signals = ["logs", "metrics", "traces"];
        private const string Authorization = "Bearer local-acceptance-only";

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ConfiguredHeadersReachAllSignals(bool http)
        {
            using var environment = new ExportEnvironment();
            await using var receiver = await OtlpReceiver.StartAsync(Authorization, http, organization: "configured-tenant");
            var settings = Settings(receiver.Endpoint);
            settings["OpenTelemetry:OtlpExportProtocol"] = http ? "HttpProtobuf" : "Grpc";
            settings["OpenTelemetry:Headers:Authorization"] = Authorization;
            settings["OpenTelemetry:Headers:organization"] = "configured-tenant";

            await ExportAsync(settings);

            await receiver.AssertSignalsAsync(accepted: true);
        }

        [Fact]
        public async Task EnvironmentOverridesEndpointProtocolAndWholeHeaderSetForAllSignals()
        {
            using var environment = new ExportEnvironment();
            await using var receiver = await OtlpReceiver.StartAsync(Authorization, http: true, organization: "environment-tenant");
            await using var unused = await OtlpReceiver.StartAsync("unused-local-credential");
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", receiver.Endpoint + "/tenant");
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf");
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS", "Authorization=Bearer%20local-acceptance-only,organization=environment-tenant");
            var settings = Settings(unused.Endpoint);
            settings["OpenTelemetry:Headers:Authorization"] = "unused-local-credential";
            settings["OpenTelemetry:Headers:x-config-only"] = "must-not-be-forwarded";
            settings["OpenTelemetry:Headers:organization"] = "unused-tenant";

            await ExportAsync(settings);

            await receiver.AssertSignalsAsync(accepted: true);
            Assert.Empty(unused.Received);
        }

        [Fact]
        public async Task EnvironmentEndpointWorksWithoutModuleEndpoint()
        {
            using var environment = new ExportEnvironment();
            await using var receiver = await OtlpReceiver.StartAsync(Authorization);
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", receiver.Endpoint);
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS", "Authorization=Bearer%20local-acceptance-only");
            var settings = Settings(receiver.Endpoint);
            _ = settings.Remove("OpenTelemetry:Endpoint");

            await ExportAsync(settings);

            await receiver.AssertSignalsAsync(accepted: true);
        }

        [Fact]
        public void InvalidFinalSettingsAreRejectedWithoutHeaderValues()
        {
            using var environment = new ExportEnvironment();
            var settings = Settings("http://localhost:4317");
            settings["OpenTelemetry:EnableLogs"] = "false";
            settings["OpenTelemetry:Headers:Authorization"] = "local-sensitive-value,invalid";
            var builder = Host.CreateApplicationBuilder();
            _ = builder.Configuration.AddInMemoryCollection(settings);
            _ = builder.AddSitkoCore();

            var exception = Assert.Throws<OptionsValidationException>(builder.Build);

            Assert.DoesNotContain("local-sensitive-value", exception.ToString(), StringComparison.Ordinal);
            Assert.Contains("Headers", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task EmptyEnvironmentKeepsConfiguredHeadersAndEndpoint()
        {
            using var environment = new ExportEnvironment();
            await using var receiver = await OtlpReceiver.StartAsync(Authorization);
            foreach (var suffix in new[] { "ENDPOINT", "PROTOCOL", "HEADERS" })
            {
                Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_" + suffix, "");
            }

            var settings = Settings(receiver.Endpoint);
            settings["OpenTelemetry:Headers:Authorization"] = Authorization;
            await ExportAsync(settings);
            await receiver.AssertSignalsAsync(accepted: true);
        }

        [Fact]
        public async Task MissingHeadersAreRejectedByReceiverForAllSignals()
        {
            using var environment = new ExportEnvironment();
            await using var receiver = await OtlpReceiver.StartAsync(Authorization);
            await ExportAsync(Settings(receiver.Endpoint));
            await receiver.AssertSignalsAsync(accepted: false);
        }

        [Theory]
        [InlineData("export")]
        [InlineData("module")]
        [InlineData("signals")]
        public async Task EnvironmentDoesNotEnableDisabledExport(string disabled)
        {
            using var environment = new ExportEnvironment();
            await using var receiver = await OtlpReceiver.StartAsync(Authorization);
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", receiver.Endpoint);
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS", "Authorization=Bearer%20local-acceptance-only");
            var settings = Settings(receiver.Endpoint);
            if (disabled == "export")
            {
                settings["OpenTelemetry:EnableOtlpExport"] = "false";
            }
            else if (disabled == "module")
            {
                settings["OpenTelemetry:Enabled"] = "false";
            }
            else
            {
                settings["OpenTelemetry:EnableLogs"] = "false";
                settings["OpenTelemetry:EnableMetrics"] = "false";
                settings["OpenTelemetry:EnableTracing"] = "false";
            }

            await ExportAsync(settings);
            Assert.Empty(receiver.Received);
        }

        [Theory]
        [InlineData("ENDPOINT")]
        [InlineData("PROTOCOL")]
        [InlineData("HEADERS")]
        public void InvalidEnvironmentIsRejectedWithoutValues(string suffix)
        {
            using var environment = new ExportEnvironment();
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_" + suffix, "local-sensitive-value");
            var builder = Host.CreateApplicationBuilder();
            _ = builder.Configuration.AddInMemoryCollection(Settings("http://localhost:4317"));
            _ = builder.AddSitkoCore();

            var exception = Assert.Throws<OptionsValidationException>(builder.Build);

            Assert.DoesNotContain("local-sensitive-value", exception.ToString(), StringComparison.Ordinal);
        }

        [Fact]
        public async Task UnsupportedSignalOverridesDoNotReplaceCommonSettings()
        {
            using var environment = new ExportEnvironment();
            await using var receiver = await OtlpReceiver.StartAsync(Authorization);
            foreach (var signal in new[] { "LOGS", "METRICS", "TRACES" })
            {
                Environment.SetEnvironmentVariable($"OTEL_EXPORTER_OTLP_{signal}_ENDPOINT", "http://127.0.0.1:1");
                Environment.SetEnvironmentVariable($"OTEL_EXPORTER_OTLP_{signal}_PROTOCOL", "http/protobuf");
                Environment.SetEnvironmentVariable($"OTEL_EXPORTER_OTLP_{signal}_HEADERS", "Authorization=unused-local-credential");
            }

            var settings = Settings(receiver.Endpoint);
            settings["OpenTelemetry:Headers:Authorization"] = Authorization;
            await ExportAsync(settings);
            await receiver.AssertSignalsAsync(accepted: true);
        }

        [Fact]
        public void ValidatorFailuresDoNotRetainHeaderValues()
        {
            using var environment = new ExportEnvironment();
            var options = new OpenTelemetryModuleOptions
            {
                EnableOtlpExport = true,
                Endpoint = new Uri("http://localhost:4317"),
                Headers = new Dictionary<string, string> { ["Authorization"] = "local-sensitive-value\r\ninvalid" }
            };
            var result = new OpenTelemetryModuleOptionsValidator().Validate(options);

            Assert.False(result.IsValid);
            Assert.All(result.Errors, static failure => Assert.Null(failure.AttemptedValue));
            Assert.DoesNotContain("local-sensitive-value", result.ToString(), StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("logs")]
        [InlineData("metrics")]
        [InlineData("traces")]
        public async Task IndividualSignalFlagsKeepOtherExportsWorking(string disabled)
        {
            using var environment = new ExportEnvironment();
            await using var receiver = await OtlpReceiver.StartAsync(Authorization, expectedSignals: 2);
            var settings = Settings(receiver.Endpoint);
            settings["OpenTelemetry:Headers:Authorization"] = Authorization;
            settings["OpenTelemetry:" + (disabled == "logs" ? "EnableLogs" :
                disabled == "metrics" ? "EnableMetrics" : "EnableTracing")] = "false";

            await ExportAsync(settings);

            await receiver.AssertSignalsAsync(accepted: true,
                [.. Signals.Where(signal => signal != disabled)]);
        }

        [Fact]
        public async Task ResourceAndMetricEnvironmentRemainEffectiveWithSdkCallback()
        {
            using var environment = new ExportEnvironment();
            Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", "acceptance.resource=local-resource-value");
            Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", "local-service-name");
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_METRICS_TEMPORALITY_PREFERENCE", "Delta");
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_METRICS_DEFAULT_HISTOGRAM_AGGREGATION", "base2_exponential_bucket_histogram");
            await using var receiver = await OtlpReceiver.StartAsync(Authorization);
            var settings = Settings(receiver.Endpoint);
            settings["OpenTelemetry:Headers:Authorization"] = Authorization;

            await ExportAsync(settings);

            await receiver.AssertSignalsAsync(accepted: true);
            Assert.All(receiver.Payloads, static signal =>
            {
                var text = Encoding.UTF8.GetString(signal.Value);
                Assert.True(text.Contains("acceptance.resource", StringComparison.Ordinal));
                Assert.True(text.Contains("local-resource-value", StringComparison.Ordinal));
                var serviceName = signal.Key == "logs" ? "local-service-name" : "otel-acceptance";
                Assert.True(text.Contains(serviceName, StringComparison.Ordinal), "service.name precedence: " + signal.Key);
            });
            var metrics = Messages(receiver.Payloads["metrics"], 1)
                .SelectMany(static resource => Messages(resource, 2))
                .SelectMany(static scope => Messages(scope, 2)).ToArray();
            var counter = metrics.Single(static metric => Encoding.UTF8.GetString(Messages(metric, 1).Single()) == Marker + "-metric");
            var sum = Messages(counter, 7).Single();
            using var input = new CodedInputStream(sum);
            var temporality = 0;
            while (input.ReadTag() is var tag && tag != 0)
            {
                if (WireFormat.GetTagFieldNumber(tag) == 2)
                {
                    temporality = input.ReadEnum();
                }
                else
                {
                    input.SkipLastField();
                }
            }

            Assert.Equal(1, temporality); // OTLP AggregationTemporality.DELTA.
            var histogram = metrics.Single(static metric => Encoding.UTF8.GetString(Messages(metric, 1).Single()) == Marker + "-histogram");
            _ = Assert.Single(Messages(histogram, 10)); // OTLP Metric.exponential_histogram.
        }

        private static IEnumerable<byte[]> Messages(byte[] message, int field)
        {
            using var input = new CodedInputStream(message);
            while (input.ReadTag() is var tag && tag != 0)
            {
                if (WireFormat.GetTagFieldNumber(tag) == field)
                {
                    yield return input.ReadBytes().ToByteArray();
                }
                else
                {
                    input.SkipLastField();
                }
            }
        }

        [Fact]
        public async Task EnvironmentHeadersWithTrailingCommaReachAllSignals()
        {
            using var environment = new ExportEnvironment();
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS", "Authorization=Bearer%20local-acceptance-only,");
            await using var receiver = await OtlpReceiver.StartAsync(Authorization);

            await ExportAsync(Settings(receiver.Endpoint));

            await receiver.AssertSignalsAsync(accepted: true);
        }

        [Theory]
        [InlineData("relative-endpoint")]
        [InlineData("endpoint-scheme")]
        [InlineData("undefined-protocol")]
        [InlineData("header-name")]
        public void InvalidModuleParametersAreRejectedSafely(string invalid)
        {
            using var environment = new ExportEnvironment();
            var settings = Settings("http://localhost:4317");
            settings["OpenTelemetry:Headers:Authorization"] = "local-sensitive-value";
            switch (invalid)
            {
                case "relative-endpoint":
                    settings["OpenTelemetry:Endpoint"] = "/relative";
                    break;
                case "endpoint-scheme":
                    settings["OpenTelemetry:Endpoint"] = "ftp://localhost";
                    break;
                case "undefined-protocol":
                    settings["OpenTelemetry:OtlpExportProtocol"] = "99";
                    break;
                case "header-name":
                    settings["OpenTelemetry:Headers:invalid name"] = "local-sensitive-value";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(invalid));
            }

            var builder = Host.CreateApplicationBuilder();
            _ = builder.Configuration.AddInMemoryCollection(settings);
            _ = builder.AddSitkoCore();
            var exception = Assert.Throws<OptionsValidationException>(builder.Build);
            Assert.DoesNotContain("local-sensitive-value", exception.ToString(), StringComparison.Ordinal);
        }

        [Fact]
        public async Task LogsOnlyGrpcUsesEnvironmentHeaders()
        {
            using var environment = new ExportEnvironment();
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS", "Authorization=Bearer%20local-acceptance-only");
            await using var receiver = await OtlpReceiver.StartAsync(Authorization, expectedSignals: 1);
            var settings = Settings(receiver.Endpoint);
            settings["OpenTelemetry:EnableMetrics"] = "false";
            settings["OpenTelemetry:EnableTracing"] = "false";

            await ExportAsync(settings);

            await receiver.AssertSignalsAsync(accepted: true, ["logs"]);
        }

        private static Dictionary<string, string?> Settings(string endpoint) => new()
        {
            ["Application:Name"] = "otel-acceptance",
            ["Application:EnableConsoleLogging"] = "false",
            ["OpenTelemetry:EnableOtlpExport"] = "true",
            ["OpenTelemetry:Endpoint"] = endpoint,
            ["OpenTelemetry:OtlpExportProtocol"] = "Grpc"
        };

        private static async Task ExportAsync(Dictionary<string, string?> settings)
        {
            var builder = Host.CreateApplicationBuilder();
            _ = builder.Configuration.AddInMemoryCollection(settings);
            _ = builder.AddSitkoCore().ConfigureOpenTelemetry(static (_, _, telemetry) =>
                telemetry.WithMetrics(static metrics => metrics.AddMeter(Marker)));
            using var host = builder.Build();
            await host.StartAsync(TestContext.Current.CancellationToken);
            using var source = new ActivitySource("Sitko.Otlp.Acceptance");
            using (source.StartActivity(Marker + "-trace"))
            {
                host.Services.GetRequiredService<ILogger<OpenTelemetryExportTests>>()
                    .LogInformation(Marker + "-log");
            }

            using var meter = new Meter(Marker);
            meter.CreateCounter<int>(Marker + "-metric").Add(1);
            meter.CreateHistogram<double>(Marker + "-histogram").Record(5);
            _ = host.Services.GetService<TracerProvider>()?.ForceFlush();
            _ = host.Services.GetService<MeterProvider>()?.ForceFlush();
            await host.StopAsync(TestContext.Current.CancellationToken);
            await Log.CloseAndFlushAsync();
        }

        private sealed class ExportEnvironment : IDisposable
        {
            private readonly Dictionary<string, string?> previous = [];

            public ExportEnvironment()
            {
                foreach (var name in Environment.GetEnvironmentVariables().Keys.Cast<string>()
                             .Where(static name => name.StartsWith("OTEL_EXPORTER_OTLP_", StringComparison.Ordinal) ||
                                 name is "OTEL_RESOURCE_ATTRIBUTES" or "OTEL_SERVICE_NAME"))
                {
                    previous[name] = Environment.GetEnvironmentVariable(name);
                    Environment.SetEnvironmentVariable(name, null);
                }
            }

            public void Dispose()
            {
                foreach (var name in Environment.GetEnvironmentVariables().Keys.Cast<string>()
                             .Where(static name => name.StartsWith("OTEL_EXPORTER_OTLP_", StringComparison.Ordinal) ||
                                 name is "OTEL_RESOURCE_ATTRIBUTES" or "OTEL_SERVICE_NAME"))
                {
                    Environment.SetEnvironmentVariable(name, null);
                }

                foreach (var (name, value) in previous)
                {
                    Environment.SetEnvironmentVariable(name, value);
                }
            }
        }

        private sealed class OtlpReceiver(WebApplication application, string authorization, int expectedSignals, string? organization) : IAsyncDisposable
        {
            public ConcurrentDictionary<string, bool> Received { get; } = [];
            public ConcurrentDictionary<string, byte[]> Payloads { get; } = [];
            private readonly TaskCompletionSource allSignals = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public string Endpoint => application.Urls.Single();

            public static async Task<OtlpReceiver> StartAsync(string authorization, bool http = false, int expectedSignals = 3, string? organization = null)
            {
                var builder = WebApplication.CreateBuilder();
                _ = builder.Logging.ClearProviders();
                _ = builder.WebHost.ConfigureKestrel(options =>
                    options.Listen(IPAddress.Loopback, 0, listen =>
                        listen.Protocols = http ? HttpProtocols.Http1 : HttpProtocols.Http2));
                var app = builder.Build();
                var receiver = new OtlpReceiver(app, authorization, expectedSignals, organization);
                app.Run(receiver.ReceiveAsync);
                await app.StartAsync(TestContext.Current.CancellationToken);
                return receiver;
            }

            private async Task ReceiveAsync(HttpContext context)
            {
                using var body = new MemoryStream();
                await context.Request.Body.CopyToAsync(body, context.RequestAborted);
                var text = Encoding.UTF8.GetString(body.ToArray());
                var path = context.Request.Path.Value!;
                var signal = path.Contains("LogsService", StringComparison.Ordinal) ? "logs" :
                    path.Contains("MetricsService", StringComparison.Ordinal) ? "metrics" :
                    path.Contains("TraceService", StringComparison.Ordinal) ? "traces" :
                    path.EndsWith("/v1/logs", StringComparison.Ordinal) ? "logs" :
                    path.EndsWith("/v1/metrics", StringComparison.Ordinal) ? "metrics" :
                    path.EndsWith("/v1/traces", StringComparison.Ordinal) ? "traces" : "unknown";
                var accepted = context.Request.Headers.Authorization == authorization
                               && !context.Request.Headers.ContainsKey("x-config-only")
                               && (organization is null || context.Request.Headers["organization"] == organization);
                var expectedMarker = signal switch
                {
                    "logs" => Marker + "-log",
                    "metrics" => Marker + "-metric",
                    "traces" => Marker + "-trace",
                    _ => Marker + "-unknown"
                };
                if (text.Contains(expectedMarker, StringComparison.Ordinal))
                {
                    Received[signal] = accepted;
                    Payloads[signal] = context.Request.Protocol == "HTTP/2" ? body.ToArray()[5..] : body.ToArray();
                    if (Received.Count == expectedSignals)
                    {
                        _ = allSignals.TrySetResult();
                    }
                }

                if (!accepted)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }

                if (path.StartsWith("/tenant", StringComparison.Ordinal))
                {
                    Assert.Equal("/tenant/v1/" + signal, path);
                }

                if (context.Request.Protocol != "HTTP/2")
                {
                    context.Response.ContentType = "application/x-protobuf";
                    return;
                }

                context.Response.ContentType = "application/grpc";
                context.Response.DeclareTrailer("grpc-status");
                await context.Response.Body.WriteAsync(new byte[5], context.RequestAborted);
                context.Response.AppendTrailer("grpc-status", "0");
            }

            public async Task AssertSignalsAsync(bool accepted, string[]? signals = null)
            {
                await allSignals.Task.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken);
                Assert.Equal(signals ?? ["logs", "metrics", "traces"], Received.Keys.Order().ToArray());
                Assert.All(Received.Values, value => Assert.Equal(accepted, value));
            }

            public async ValueTask DisposeAsync() => await application.DisposeAsync();
        }
    }
}
