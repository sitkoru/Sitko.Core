using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.MessageBus;
using Sitko.Core.Queue.Tests;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Queue.Nats.Tests
{
    public class MessageBusTests : BaseTest<NatsMessageBusTestScope>
    {
        public MessageBusTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task TranslateNotification()
        {
            var scope = GetScope();

            var queue = scope.Get<IQueue>();

            Guid? receivedId = null;
            var sub = await queue.SubscribeAsync<TestRequest>((testRequest, context) =>
            {
                receivedId = testRequest.Id;
                return Task.FromResult(true);
            });
            Assert.True(sub.IsSuccess);

            var mediator = scope.Get<IMediator>();
            var request = new TestRequest();
            await mediator.Publish(request);

            await Task.Delay(TimeSpan.FromSeconds(1));

            Assert.NotNull(receivedId);
            Assert.Equal(request.Id, receivedId);
        }
    }

    public class NatsMessageBusTestScope : NatsQueueTestScope
    {
        protected override void ConfigureQueue(NatsQueueModuleConfig config, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureQueue(config, configuration, environment);
            config.TranslateMessageBusNotification<TestRequest>();
        }

        protected override TestApplication ConfigureApplication(TestApplication application, string name)
        {
            return base.ConfigureApplication(application, name)
                .AddModule<MessageBusModule<MessageBusTests>, MessageBusModuleConfig<MessageBusTests>>();
        }
    }

    public class TestRequest : TestMessage, INotification
    {
    }
}
