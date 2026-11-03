using Confluent.Kafka;
using KafkaFlow;
using KafkaFlow.Producers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Polly;
using Sitko.Core.App;
using Sitko.Core.Kafka;
using Sitko.Core.Queue.Kafka.Tests.Data;
using Xunit;

namespace Sitko.Core.Queue.Kafka.Tests
{
    public sealed class EventProducerContractTests
    {
        [Fact]
        public async Task GlobalPolicyRetriesQueueFullAndPreservesMessageAndReceipt()
        {
            var transport = new ControlledProducer();
            var error = QueueFull();
            var report = Receipt();
            transport.Attempts.Enqueue(Task.FromException<DeliveryResult<byte[], byte[]>>(error));
            transport.Attempts.Enqueue(Task.FromResult(report));
            using var host = CreateHost(transport, services =>
                services.AddKafkaProducerResiliencePipeline(options =>
                {
                    options.MaxRetryAttempts = 1;
                    options.Delay = TimeSpan.Zero;
                }));
            var producer = host.Services.GetRequiredService<IEventProducer>();
            var message = new TestEvent();

            Assert.Same(report, await producer.ProduceAsync("retry-key", message));
            Assert.Equal(2, transport.SendCount);
            Assert.All(transport.Sends, send =>
            {
                Assert.Equal("contract-TestEvents", send.Topic);
                Assert.Equal("retry-key", send.Key);
                Assert.Same(message, send.Value);
            });
        }

        [Theory]
        [InlineData(false, false, 2)]
        [InlineData(false, true, 2)]
        [InlineData(true, false, 1)]
        [InlineData(true, true, 1)]
        public async Task EventPolicyOverridesOrDisablesGlobalRegardlessOfRegistrationOrder(
            bool disable, bool globalFirst, int expectedSends)
        {
            var transport = new ControlledProducer();
            var error = QueueFull();
            transport.Completion.SetException(error);
            using var host = CreateHost(transport, services =>
            {
                void AddGlobal() => services.AddKafkaProducerResiliencePipeline(options =>
                {
                    options.MaxRetryAttempts = 3;
                    options.Delay = TimeSpan.Zero;
                });
                if (globalFirst)
                {
                    AddGlobal();
                }

                if (disable)
                {
                    _ = services.DisableKafkaProducerResiliencePipeline<TestEvent>();
                }
                else
                {
                    _ = services.AddKafkaProducerResiliencePipeline<TestEvent>(options =>
                    {
                        options.MaxRetryAttempts = 1;
                        options.Delay = TimeSpan.Zero;
                    });
                }

                if (!globalFirst)
                {
                    AddGlobal();
                }
            });
            var producer = host.Services.GetRequiredService<IEventProducer>();

            var observed = await Assert.ThrowsAsync<ProduceException<byte[], byte[]>>(() =>
                producer.ProduceAsync("key", new TestEvent()));
            Assert.Same(error, observed);
            Assert.Same(error.DeliveryResult, observed.DeliveryResult);
            Assert.Equal(expectedSends, transport.SendCount);
        }

        [Theory]
        [InlineData(ErrorCode.Local_MsgTimedOut)]
        [InlineData(ErrorCode.Local_TimedOut)]
        [InlineData(ErrorCode.Local_Transport)]
        [InlineData(ErrorCode.RequestTimedOut)]
        [InlineData(ErrorCode.Local_KeySerialization)]
        [InlineData(ErrorCode.Local_ValueSerialization)]
        [InlineData(ErrorCode.TopicAuthorizationFailed)]
        [InlineData(ErrorCode.MsgSizeTooLarge)]
        [InlineData(ErrorCode.InvalidRequiredAcks)]
        [InlineData(ErrorCode.Unknown)]
        public async Task BroadPolicyCannotRetryUnapprovedErrorEvenWhenStatusIsNotPersisted(ErrorCode code)
        {
            var transport = new ControlledProducer();
            var error = new ProduceException<byte[], byte[]>(new Error(code), QueueFull().DeliveryResult);
            transport.Completion.SetException(error);
            using var host = CreateHost(transport, AddBroadPolicy);

            var observed = await Assert.ThrowsAsync<ProduceException<byte[], byte[]>>(() =>
                host.Services.GetRequiredService<IEventProducer>().ProduceAsync("key", new TestEvent()));
            Assert.Same(error, observed);
            Assert.Same(error.DeliveryResult, observed.DeliveryResult);
            Assert.Equal(1, transport.SendCount);
        }

        [Theory]
        [InlineData("possibly-persisted")]
        [InlineData("persisted")]
        [InlineData("unknown-status")]
        [InlineData("missing-report")]
        [InlineData("empty-topic")]
        [InlineData("wrong-topic")]
        [InlineData("invalid-partition")]
        [InlineData("unexpected-offset")]
        [InlineData("fatal")]
        public async Task BroadPolicyCannotRetryUnreliableQueueFullReport(string scenario)
        {
            var report = QueueFull().DeliveryResult;
            switch (scenario)
            {
                case "possibly-persisted":
                    report.Status = PersistenceStatus.PossiblyPersisted;
                    break;
                case "persisted":
                    report.Status = PersistenceStatus.Persisted;
                    break;
                case "unknown-status":
                    report.Status = (PersistenceStatus)99;
                    break;
                case "empty-topic":
                    report.Topic = " ";
                    break;
                case "wrong-topic":
                    report.Topic = "another-topic";
                    break;
                case "invalid-partition":
                    report.Partition = -2;
                    break;
                case "unexpected-offset":
                    report.Offset = 0;
                    break;
                case "fatal":
                case "missing-report":
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(scenario));
            }

            var error = new ProduceException<byte[], byte[]>(
                new Error(ErrorCode.Local_QueueFull, "queue full", scenario == "fatal"),
                scenario == "missing-report" ? null : report);
            var transport = new ControlledProducer();
            transport.Completion.SetException(error);
            using var host = CreateHost(transport, AddBroadPolicy);

            var observed = await Assert.ThrowsAsync<ProduceException<byte[], byte[]>>(() =>
                host.Services.GetRequiredService<IEventProducer>().ProduceAsync("key", new TestEvent()));
            Assert.Same(error, observed);
            Assert.Equal(1, transport.SendCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task BroadPolicyPreservesUnknownOrCancelledOutcomeWithoutRetry(bool cancelled)
        {
            var transport = new ControlledProducer();
            Exception error = cancelled ? new OperationCanceledException() : new InvalidOperationException("after write");
            transport.Completion.SetException(error);
            using var host = CreateHost(transport, AddBroadPolicy);

            var observed = await Record.ExceptionAsync(() =>
                host.Services.GetRequiredService<IEventProducer>().ProduceAsync("key", new TestEvent()));
            Assert.Same(error, observed);
            Assert.Equal(1, transport.SendCount);
        }

        [Theory]
        [InlineData("success")]
        [InlineData("missing")]
        [InlineData("not-persisted")]
        [InlineData("possibly-persisted")]
        [InlineData("invalid-coordinates")]
        public async Task BroadResultPredicateNeverRepeatsReturnedReport(string scenario)
        {
            var report = Receipt();
            switch (scenario)
            {
                case "not-persisted":
                    report.Status = PersistenceStatus.NotPersisted;
                    break;
                case "possibly-persisted":
                    report.Status = PersistenceStatus.PossiblyPersisted;
                    break;
                case "invalid-coordinates":
                    report.Offset = Offset.Unset;
                    break;
                case "success":
                case "missing":
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(scenario));
            }

            var transport = new ControlledProducer();
            transport.Completion.SetResult(scenario == "missing" ? null! : report);
            using var host = CreateHost(transport, AddBroadPolicy);
            var producer = host.Services.GetRequiredService<IEventProducer>();

            if (scenario == "success")
            {
                Assert.Same(report, await producer.ProduceAsync("key", new TestEvent()));
            }
            else
            {
                _ = await Assert.ThrowsAsync<InvalidOperationException>(() => producer.ProduceAsync("key", new TestEvent()));
            }

            Assert.Equal(1, transport.SendCount);
        }

        [Fact]
        public async Task ExhaustionPreservesLastTransportExceptionAndDeliveryReport()
        {
            var transport = new ControlledProducer();
            transport.Attempts.Enqueue(Task.FromException<DeliveryResult<byte[], byte[]>>(QueueFull()));
            transport.Attempts.Enqueue(Task.FromException<DeliveryResult<byte[], byte[]>>(QueueFull()));
            var finalError = QueueFull();
            transport.Completion.SetException(finalError);
            using var host = CreateHost(transport, AddBroadPolicy);

            var observed = await Assert.ThrowsAsync<ProduceException<byte[], byte[]>>(() =>
                host.Services.GetRequiredService<IEventProducer>().ProduceAsync("key", new TestEvent()));
            Assert.Same(finalError, observed);
            Assert.Same(finalError.DeliveryResult, observed.DeliveryResult);
            Assert.Equal(3, transport.SendCount);
        }

        [Theory]
        [InlineData("none")]
        [InlineData("predicate-rejects")]
        [InlineData("different-event")]
        [InlineData("disabled-only")]
        [InlineData("arbitrary-pipeline")]
        public async Task QueueFullIsNotRetriedWithoutApplicablePolicy(string scenario)
        {
            var transport = new ControlledProducer();
            var error = QueueFull();
            transport.Completion.SetException(error);
            using var host = CreateHost(transport, services =>
            {
                switch (scenario)
                {
                    case "predicate-rejects":
                        _ = services.AddKafkaProducerResiliencePipeline(options =>
                            options.ShouldHandle = static _ => ValueTask.FromResult(false));
                        break;
                    case "different-event":
                        _ = services.AddKafkaProducerResiliencePipeline<RetryTestEvent>(static options => options.Delay = TimeSpan.Zero);
                        break;
                    case "disabled-only":
                        _ = services.DisableKafkaProducerResiliencePipeline<TestEvent>();
                        break;
                    case "arbitrary-pipeline":
                        _ = services.AddResiliencePipeline("KafkaProducer", static builder => builder.AddTimeout(TimeSpan.FromMilliseconds(1)));
                        break;
                    case "none":
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(scenario));
                }
            });

            var observed = await Assert.ThrowsAsync<ProduceException<byte[], byte[]>>(() =>
                host.Services.GetRequiredService<IEventProducer>().ProduceAsync("key", new TestEvent()));
            Assert.Same(error, observed);
            Assert.Equal(1, transport.SendCount);
        }

        [Fact]
        public async Task PolicyUsesDeclaredEventTypeLikeRouting()
        {
            var transport = new ControlledProducer();
            transport.Attempts.Enqueue(Task.FromException<DeliveryResult<byte[], byte[]>>(QueueFull()));
            var report = Receipt();
            transport.Completion.SetResult(report);
            using var host = CreateHost(transport, services =>
            {
                _ = services.AddKafkaProducerResiliencePipeline<TestEvent>(static options => options.Delay = TimeSpan.Zero);
                _ = services.DisableKafkaProducerResiliencePipeline<DerivedTestEvent>();
            });
            TestEvent message = new DerivedTestEvent();

            Assert.Same(report, await host.Services.GetRequiredService<IEventProducer>().ProduceAsync("key", message));
            Assert.Equal(2, transport.SendCount);
            Assert.All(transport.Sends, send => Assert.Same(message, send.Value));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task LastRegistrationForSameScopeWins(bool disableLast)
        {
            var transport = new ControlledProducer();
            var error = QueueFull();
            transport.Completion.SetException(error);
            using var host = CreateHost(transport, services =>
            {
                _ = services.DisableKafkaProducerResiliencePipeline<TestEvent>();
                _ = services.AddKafkaProducerResiliencePipeline<TestEvent>(static options =>
                {
                    options.MaxRetryAttempts = 1;
                    options.Delay = TimeSpan.Zero;
                });
                if (disableLast)
                {
                    _ = services.DisableKafkaProducerResiliencePipeline<TestEvent>();
                }
            });

            Assert.Same(error, await Record.ExceptionAsync(() =>
                host.Services.GetRequiredService<IEventProducer>().ProduceAsync("key", new TestEvent())));
            Assert.Equal(disableLast ? 1 : 2, transport.SendCount);
        }

        [Fact]
        public async Task RetryWaitsForCompletedAttemptAndConfiguredRetryCallback()
        {
            var transport = new ControlledProducer();
            var firstAttempt = new TaskCompletionSource<DeliveryResult<byte[], byte[]>>(TaskCreationOptions.RunContinuationsAsynchronously);
            transport.Attempts.Enqueue(firstAttempt.Task);
            var report = Receipt();
            transport.Completion.SetResult(report);
            var retryEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseRetry = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var host = CreateHost(transport, services =>
                services.AddKafkaProducerResiliencePipeline<TestEvent>(options =>
                {
                    options.Delay = TimeSpan.Zero;
                    options.OnRetry = async _ =>
                    {
                        retryEntered.SetResult();
                        await releaseRetry.Task;
                    };
                }));
            var sending = host.Services.GetRequiredService<IEventProducer>().ProduceAsync("key", new TestEvent());
            Assert.Equal(1, transport.SendCount);
            Assert.False(sending.IsCompleted);
            firstAttempt.SetException(QueueFull());
            await retryEntered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.Equal(1, transport.SendCount);
            Assert.False(sending.IsCompleted);
            releaseRetry.SetResult();
            Assert.Same(report, await sending);
            Assert.Equal(2, transport.SendCount);
        }

        [Fact]
        public void VoidProduceDoesNotUseConfiguredRetryPolicy()
        {
            var transport = new ControlledProducer { VoidError = QueueFull() };
            using var host = CreateHost(transport, AddBroadPolicy);
            var message = new TestEvent();

            Assert.Same(transport.VoidError, Record.Exception(() =>
                host.Services.GetRequiredService<IEventProducer>().Produce("key", message)));
            Assert.Equal(1, transport.VoidSendCount);
            Assert.Equal(0, transport.SendCount);
            Assert.Equal("contract-TestEvents", transport.Topic);
            Assert.Equal("key", transport.Key);
            Assert.Same(message, transport.Value);
        }

        private static void AddBroadPolicy(IServiceCollection services) =>
            services.AddKafkaProducerResiliencePipeline(static options =>
            {
                options.MaxRetryAttempts = 2;
                options.Delay = TimeSpan.Zero;
                options.ShouldHandle = static _ => ValueTask.FromResult(true);
            });

        private sealed class DerivedTestEvent : TestEvent;

        private static ProduceException<byte[], byte[]> QueueFull() => new(
            new Error(ErrorCode.Local_QueueFull), new DeliveryResult<byte[], byte[]>
            {
                Status = PersistenceStatus.NotPersisted,
                Topic = "contract-TestEvents",
                Partition = Partition.Any,
                Offset = Offset.Unset
            });

        private static DeliveryResult<byte[], byte[]> Receipt() => new()
        {
            Status = PersistenceStatus.Persisted,
            Topic = "contract-TestEvents",
            Partition = 0,
            Offset = 42
        };

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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WaitCancellationLeavesSingleSendObservable(bool policyEnabled)
        {
            var transport = new ControlledProducer();
            using var host = CreateHost(transport, policyEnabled ? AddBroadPolicy : null);
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

        private static IHost CreateHost(ControlledProducer transport, Action<IServiceCollection>? configure = null)
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
            configure?.Invoke(builder.Services);
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
            public Queue<Task<DeliveryResult<byte[], byte[]>>> Attempts { get; } = new();
            public List<(string Topic, object Key, object Value)> Sends { get; } = [];
            public Exception? VoidError { get; init; }
            public int VoidSendCount { get; private set; }
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
                Sends.Add((topic, messageKey, messageValue));
                return Attempts.Count > 0 ? Attempts.Dequeue() : Completion.Task;
            }

            public Task<DeliveryResult<byte[], byte[]>> ProduceAsync(object messageKey, object messageValue,
                IMessageHeaders? headers = null, int? partition = null) => throw new NotSupportedException();

            public void Produce(string topic, object messageKey, object messageValue, IMessageHeaders? headers = null,
                Action<DeliveryReport<byte[], byte[]>>? deliveryHandler = null, int? partition = null)
            {
                VoidSendCount++;
                Topic = topic;
                Key = messageKey;
                Value = messageValue;
                if (VoidError is not null)
                {
                    throw VoidError;
                }
            }

            public void Produce(object messageKey, object messageValue, IMessageHeaders? headers = null,
                Action<DeliveryReport<byte[], byte[]>>? deliveryHandler = null, int? partition = null) =>
                throw new NotSupportedException();

            public Task<IReadOnlyCollection<BatchProduceItem>> BatchProduceAsync(
                IReadOnlyCollection<BatchProduceItem> items, bool throwIfAnyProduceFail = true) =>
                throw new NotSupportedException();
        }
    }
}
