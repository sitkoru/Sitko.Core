using Confluent.Kafka;
using FluentValidation;
using KafkaFlow;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.Queue.Kafka.Producing;
using Acks=Confluent.Kafka.Acks;
using SecurityProtocol=KafkaFlow.Configuration.SecurityProtocol;

namespace Sitko.Core.Queue.Kafka;

public class KafkaModule : BaseApplicationModule<KafkaModuleOptions>
{
    private static readonly Dictionary<string, KafkaConfigurator> Configurators = new();
    public override bool AllowMultiple => false;

    public override string OptionsKey => "Kafka";

    public static KafkaConfigurator CreateConfigurator(string name) =>
        Configurators.SafeGetOrAdd(name, _ => new KafkaConfigurator(name));

    public override void PostConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        KafkaModuleOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.AddSingleton<KafkaConsumerOffsetsEnsurer>();
        services.AddSingleton<IEventProducer, EventProducer>();
        services.AddKafkaFlowHostedService(builder =>
        {
            foreach (var (_, configurator) in Configurators)
            {
                configurator.Build(builder, startupOptions);
            }
        });
    }

    public override async Task InitAsync(IApplicationContext applicationContext, IServiceProvider serviceProvider)
    {
        await base.InitAsync(applicationContext, serviceProvider);
        var offsetsEnsurer = serviceProvider.GetRequiredService<KafkaConsumerOffsetsEnsurer>();
        var options = GetOptions(serviceProvider);
        foreach (var (_, configurator) in Configurators)
        {
            if (configurator.NeedToEnsureOffsets)
            {
                await offsetsEnsurer.EnsureOffsetsAsync(configurator, options);
            }
        }
    }
}

public class KafkaModuleOptions : BaseModuleOptions
{
    public string[] Brokers { get; set; } = Array.Empty<string>();
    public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromSeconds(15);
    public TimeSpan MaxPollInterval { get; set; } = TimeSpan.FromMinutes(5);
    public bool UseSaslAuth { get; set; }
    public string SaslUserName { get; set; } = "";
    public string SaslPassword { get; set; } = "";
    public string SaslCertBase64 { get; set; } = "";
    public KafkaFlow.Configuration.SaslMechanism SaslMechanisms { get; set; } = KafkaFlow.Configuration.SaslMechanism.ScramSha512;
    public SecurityProtocol? SecurityProtocol { get; set; } = KafkaFlow.Configuration.SecurityProtocol.Plaintext;
    public int MaxPartitionFetchBytes { get; set; } = 5 * 1024 * 1024;
    public Confluent.Kafka.AutoOffsetReset AutoOffsetReset { get; set; } = Confluent.Kafka.AutoOffsetReset.Latest;

    public PartitionAssignmentStrategy PartitionAssignmentStrategy { get; set; } =
        PartitionAssignmentStrategy.Range;

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
            .When(options => options.SecurityProtocol == SecurityProtocol.SaslSsl).WithMessage("Specify kafka sasl certificate");
    }
}
