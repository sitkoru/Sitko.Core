using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Retry;

namespace Sitko.Core.Queue.Kafka
{
    public static class KafkaProducerResilienceExtensions
    {
        /// <summary>Configures optional retries for confirmed publishing of all event types.</summary>
        public static IServiceCollection AddKafkaProducerResiliencePipeline(this IServiceCollection services,
            Action<RetryStrategyOptions> configure) => AddPipeline(services, null, configure);

        /// <summary>Overrides the common retry policy for the declared event type.</summary>
        public static IServiceCollection AddKafkaProducerResiliencePipeline<TEvent>(this IServiceCollection services,
            Action<RetryStrategyOptions> configure) => AddPipeline(services, typeof(TEvent), configure);

        /// <summary>Disables additional retries for the declared event type, including the common policy.</summary>
        public static IServiceCollection DisableKafkaProducerResiliencePipeline<TEvent>(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            return services.AddSingleton(new KafkaProducerResilienceRegistration(typeof(TEvent), false));
        }

        private static IServiceCollection AddPipeline(IServiceCollection services, Type? eventType,
            Action<RetryStrategyOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configure);
            var registration = new KafkaProducerResilienceRegistration(eventType, true);
            _ = services.AddSingleton(registration);
            return services.AddResiliencePipeline(registration, builder =>
            {
                var options = new RetryStrategyOptions();
                configure(options);
                var shouldHandle = options.ShouldHandle;
                options.ShouldHandle = async args => IsSafeToRetry(args) && await shouldHandle(args);
                _ = builder.AddRetry(options);
            });
        }

        private static bool IsSafeToRetry(RetryPredicateArguments<object> args) =>
            args.Outcome.Exception is ProduceException<byte[], byte[]>
            {
                Error: { Code: ErrorCode.Local_QueueFull, IsFatal: false },
                DeliveryResult: { Status: PersistenceStatus.NotPersisted } report
            } && !string.IsNullOrWhiteSpace(report.Topic) && report.Topic == args.Context.OperationKey &&
            report.Partition.Value >= Partition.Any.Value && report.Offset == Offset.Unset;
    }

    internal sealed class KafkaProducerResilienceRegistration(Type? eventType, bool enabled)
    {
        public Type? EventType { get; } = eventType;
        public bool Enabled { get; } = enabled;
        public override string ToString() => EventType?.FullName ?? "KafkaProducer";
    }
}
