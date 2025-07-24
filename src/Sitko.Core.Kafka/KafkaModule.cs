using Confluent.Kafka;
using FluentValidation;
using KafkaFlow;
using KafkaFlow.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using Polly;
using Polly.Retry;
using Sitko.Core.App;
using Sitko.Core.App.OpenTelemetry;
using Acks = Confluent.Kafka.Acks;
using AutoOffsetReset = Confluent.Kafka.AutoOffsetReset;
using SaslMechanism = KafkaFlow.Configuration.SaslMechanism;
using SecurityProtocol = KafkaFlow.Configuration.SecurityProtocol;

namespace Sitko.Core.Kafka;

public class KafkaModule : BaseApplicationModule<KafkaModuleOptions>, IOpenTelemetryModule<KafkaModuleOptions>
{
    private static readonly Dictionary<string, KafkaConfigurator> Configurators = new();

    public override string OptionsKey => "Kafka";
    public override bool AllowMultiple => false;

    public override void PostConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        KafkaModuleOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.AddSingleton<KafkaConsumerOffsetsEnsurer>();
        services.AddKafkaFlowHostedService(builder =>
        {
            foreach (var (_, configurator) in Configurators)
            {
                configurator.Build(builder, startupOptions);
            }
        });
        services.AddResiliencePipeline(nameof(KafkaConsumerOffsetsEnsurer), builder =>
        {
            builder.AddRetry(new RetryStrategyOptions
            {
                UseJitter = true,
                BackoffType = DelayBackoffType.Exponential,
                MaxRetryAttempts = 10,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(exception =>
                    exception.Message.Contains("Not leader for partition", StringComparison.OrdinalIgnoreCase))
            });
        });
    }

    public OpenTelemetryBuilder ConfigureOpenTelemetry(IApplicationContext context, KafkaModuleOptions options,
        OpenTelemetryBuilder builder) =>
        builder.WithTracing(providerBuilder =>
        {
            providerBuilder.AddSource(KafkaFlowInstrumentation.ActivitySourceName);
        });

    public override async Task InitAsync(IApplicationContext applicationContext, IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        await base.InitAsync(applicationContext, serviceProvider, cancellationToken);

        var options = GetOptions(serviceProvider);
        foreach (var (_, configurator) in Configurators)
        {
            if (configurator.NeedToEnsureOffsets)
            {
                await offsetsEnsurer.EnsureOffsetsAsync(configurator, options);
            }
        }
    }

    public static KafkaConfigurator CreateConfigurator(string name) =>
        Configurators.SafeGetOrAdd(name, _ => new KafkaConfigurator(name));
}

public class KafkaModuleOptions : BaseModuleOptions
{
    public string[] Brokers { get; set; } = [];
    public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromSeconds(15);
    public TimeSpan MaxPollInterval { get; set; } = TimeSpan.FromMinutes(5);
    public bool UseSaslAuth { get; set; }
    public string SaslUserName { get; set; } = "";
    public string SaslPassword { get; set; } = "";
    public string SaslCertBase64 { get; set; } = "";
    public SaslMechanism SaslMechanisms { get; set; } = SaslMechanism.ScramSha512;
    public SecurityProtocol? SecurityProtocol { get; set; } = KafkaFlow.Configuration.SecurityProtocol.Plaintext;
    public int MaxPartitionFetchBytes { get; set; } = 5 * 1024 * 1024;
    public AutoOffsetReset AutoOffsetReset { get; set; } = AutoOffsetReset.Latest;

    public PartitionAssignmentStrategy PartitionAssignmentStrategy { get; set; } =
        PartitionAssignmentStrategy.CooperativeSticky;

    public TimeSpan MessageTimeout { get; set; } = TimeSpan.FromSeconds(12);
    public int MessageMaxBytes { get; set; } = 5 * 1024 * 1024;
    public TimeSpan MaxProducingTimeout { get; set; } = TimeSpan.FromMilliseconds(100);
    public bool EnableIdempotence { get; set; } = true;
    public bool SocketNagleDisable { get; set; } = true;
    public Acks Acks { get; set; } = Acks.All;
}

public class KafkaModuleOptionsValidator : AbstractValidator<KafkaModuleOptions>
{
    public KafkaModuleOptionsValidator()
    {
        RuleFor(options => options.Brokers).NotEmpty().WithMessage("Specify Kafka brokers");
        RuleFor(options => options.SaslCertBase64).NotEmpty()
            .When(options => options.SecurityProtocol == SecurityProtocol.SaslSsl)
            .WithMessage("Specify kafka sasl certificate");
    }
}
