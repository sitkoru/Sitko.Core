using KafkaFlow;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Kafka;

public class KafkaModule : BaseApplicationModule
{
    private static readonly Dictionary<string, KafkaConfigurator> Configurators = new();
    public override bool AllowMultiple => false;

    public static KafkaConfigurator CreateConfigurator(string name, string[] brokers) =>
        Configurators.SafeGetOrAdd(name, _ => new KafkaConfigurator(name, brokers));

    public override string OptionsKey => "Kafka";

    public override void PostConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        BaseApplicationModuleOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.AddKafkaFlowHostedService(builder =>
        {
            foreach (var (_, configurator) in Configurators)
            {
                configurator.Build(builder);
            }
        });
    }
}
