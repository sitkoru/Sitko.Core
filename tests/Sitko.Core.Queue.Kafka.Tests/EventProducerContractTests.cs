using Confluent.Kafka;
using KafkaFlow;
using KafkaFlow.Producers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Kafka;
using Sitko.Core.Queue.Kafka.Tests.Data;
using Xunit;

namespace Sitko.Core.Queue.Kafka.Tests
{
    public sealed class EventProducerContractTests
    {
        [Fact]
        public async Task RejectsMissingConfirmationWithoutRetry()
        {
            var transport = new ControlledProducer();
            transport.Completion.SetResult(null!);
            using var host = CreateHost(transport);
            var producer = host.Services.GetRequiredService<IEventProducer>();

            _ = await Assert.ThrowsAsync<InvalidOperationException>(() => producer.ProduceAsync("key", new TestEvent()));
            Assert.Equal(1, transport.SendCount);
        }

        [Theory]
        [InlineData(PersistenceStatus.NotPersisted, "contract-TestEvents", 0, 0)]
        [InlineData(PersistenceStatus.PossiblyPersisted, "contract-TestEvents", 0, 0)]
        [InlineData(PersistenceStatus.Persisted, "", 0, 0)]
        [InlineData(PersistenceStatus.Persisted, " ", 0, 0)]
        [InlineData(PersistenceStatus.Persisted, "contract-TestEvents", -1, 0)]
        [InlineData(PersistenceStatus.Persisted, "contract-TestEvents", 0, -1001)]
        public async Task RejectsUnconfirmedOrInvalidReportWithoutRetry(PersistenceStatus status, string topic,
            int partition, long offset)
        {
            var transport = new ControlledProducer();
            transport.Completion.SetResult(new DeliveryResult<byte[], byte[]>
            {
                Status = status,
                Topic = topic,
                Partition = partition,
                Offset = offset
            });
            using var host = CreateHost(transport);
            var producer = host.Services.GetRequiredService<IEventProducer>();

            _ = await Assert.ThrowsAsync<InvalidOperationException>(() => producer.ProduceAsync("key", new TestEvent()));
            Assert.Equal(1, transport.SendCount);
        }

        [Theory]
        [InlineData(PersistenceStatus.NotPersisted)]
        [InlineData(PersistenceStatus.PossiblyPersisted)]
        public async Task PreservesDeliveryErrorAndStatusWithoutRetry(PersistenceStatus status)
        {
            var transport = new ControlledProducer();
            var report = new DeliveryResult<byte[], byte[]> { Status = status, Topic = "contract-TestEvents" };
            var error = new ProduceException<byte[], byte[]>(new Error(ErrorCode.Local_MsgTimedOut), report);
            transport.Completion.SetException(error);
            using var host = CreateHost(transport);
            var producer = host.Services.GetRequiredService<IEventProducer>();

            var observed = await Assert.ThrowsAsync<ProduceException<byte[], byte[]>>(() =>
                producer.ProduceAsync("key", new TestEvent()));
            Assert.Same(error, observed);
            Assert.Same(report, observed.DeliveryResult);
            Assert.Equal(status, observed.DeliveryResult.Status);
            Assert.Equal(1, transport.SendCount);
        }

        [Fact]
        public async Task PreservesErrorWithoutDeliveryReportWithoutRetry()
        {
            var transport = new ControlledProducer();
            var error = new InvalidOperationException("Completion handler failed after sending");
            transport.Completion.SetException(error);
            using var host = CreateHost(transport);
            var producer = host.Services.GetRequiredService<IEventProducer>();

            var observed = await Assert.ThrowsAsync<InvalidOperationException>(() => producer.ProduceAsync("key", new TestEvent()));
            Assert.Same(error, observed);
            Assert.Equal(1, transport.SendCount);
        }

        [Fact]
        public async Task WaitCancellationLeavesSingleSendObservable()
        {
            var transport = new ControlledProducer();
            using var host = CreateHost(transport);
            var producer = host.Services.GetRequiredService<IEventProducer>();
            var message = new TestEvent();
            var sending = producer.ProduceAsync("contract-key", message);
            Assert.False(sending.IsCompleted);
            Assert.Equal("contract-TestEvents", transport.Topic);
            Assert.Equal("contract-key", transport.Key);
            Assert.Same(message, transport.Value);
            using var cancellation = new CancellationTokenSource();
            await cancellation.CancelAsync();
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sending.WaitAsync(cancellation.Token));
            Assert.False(sending.IsCompleted);

            var report = new DeliveryResult<byte[], byte[]>
            {
                Status = PersistenceStatus.Persisted,
                Topic = "contract-TestEvents",
                Partition = 0,
                Offset = 0
            };
            transport.Completion.SetResult(report);
            Assert.Same(report, await sending);
            Assert.Equal(1, transport.SendCount);
        }

        private static IHost CreateHost(ControlledProducer transport)
        {
            var builder = Host.CreateApplicationBuilder();
            var core = builder.AddSitkoCore();
            _ = core.AddModule<KafkaModule, KafkaModuleOptions>(static options => options.Brokers = ["localhost:9092"]);
            _ = core.AddKafkaQueue(static options =>
            {
                options.TopicPrefix = "contract";
                options.ProducerName = "contract-producer";
                _ = options.AddAssembly<TestEvent>();
            });
            _ = builder.Services.AddSingleton(transport);
            _ = core.AddModule<ControlledTransportModule, ControlledTransportOptions>();
            return builder.Build();
        }

        public sealed class ControlledTransportOptions : BaseModuleOptions;

        public sealed class ControlledTransportModule : BaseApplicationModule<ControlledTransportOptions>
        {
            public override string OptionsKey => "ControlledTransport";

            public override void PostConfigureServices(IApplicationContext applicationContext,
                IServiceCollection services, ControlledTransportOptions startupOptions) =>
                services.Replace(ServiceDescriptor.Singleton<IProducerAccessor>(static provider =>
                    provider.GetRequiredService<ControlledProducer>()));
        }

        private sealed class ControlledProducer : IMessageProducer, IProducerAccessor
        {
            public TaskCompletionSource<DeliveryResult<byte[], byte[]>> Completion { get; } =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            public int SendCount { get; private set; }
            public string? Topic { get; private set; }
            public object? Key { get; private set; }
            public object? Value { get; private set; }
            public string ProducerName => "contract-producer";
            public IEnumerable<IMessageProducer> All => [this];
            public IMessageProducer this[string name] => GetProducer(name);

            public IMessageProducer GetProducer(string name)
            {
                Assert.Equal(ProducerName, name);
                return this;
            }

            public IMessageProducer GetProducer<TProducer>() => throw new NotSupportedException();

            public Task<DeliveryResult<byte[], byte[]>> ProduceAsync(string topic, object messageKey,
                object messageValue, IMessageHeaders? headers = null, int? partition = null)
            {
                SendCount++;
                Topic = topic;
                Key = messageKey;
                Value = messageValue;
                return Completion.Task;
            }

            public Task<DeliveryResult<byte[], byte[]>> ProduceAsync(object messageKey, object messageValue,
                IMessageHeaders? headers = null, int? partition = null) => throw new NotSupportedException();

            public void Produce(string topic, object messageKey, object messageValue, IMessageHeaders? headers = null,
                Action<DeliveryReport<byte[], byte[]>>? deliveryHandler = null, int? partition = null) =>
                throw new NotSupportedException();

            public void Produce(object messageKey, object messageValue, IMessageHeaders? headers = null,
                Action<DeliveryReport<byte[], byte[]>>? deliveryHandler = null, int? partition = null) =>
                throw new NotSupportedException();

            public Task<IReadOnlyCollection<BatchProduceItem>> BatchProduceAsync(
                IReadOnlyCollection<BatchProduceItem> items, bool throwIfAnyProduceFail = true) =>
                throw new NotSupportedException();
        }
    }
}
