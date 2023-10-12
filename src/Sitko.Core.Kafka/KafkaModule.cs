using KafkaFlow;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Kafka;

public class KafkaModule : BaseApplicationModule
{
    private static readonly Dictionary<string, KafkaConfigurator> Configurators = new();
    public override bool AllowMultiple => false;

    public override string OptionsKey => "Kafka";

    public static KafkaConfigurator CreateConfigurator(string name, string[] brokers) =>
        Configurators.SafeGetOrAdd(name, _ => new KafkaConfigurator(name, brokers));

    public override void PostConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        BaseApplicationModuleOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.AddSingleton<KafkaConsumerOffsetsEnsurer>();
        services.AddKafkaFlowHostedService(builder =>
        {
            foreach (var (_, configurator) in Configurators)
            {
                configurator.Build(builder);
            }
        });
    }

    public override async Task InitAsync(IApplicationContext applicationContext, IServiceProvider serviceProvider)
    {
        await base.InitAsync(applicationContext, serviceProvider);
        var offsetsEnsurer = serviceProvider.GetRequiredService<KafkaConsumerOffsetsEnsurer>();
        foreach (var (_, configurator) in Configurators)
        {
            if (configurator.NeedToEnsureOffsets)
            {
                await offsetsEnsurer.EnsureOffsetsAsync(configurator);
            }
        }
    }
}
