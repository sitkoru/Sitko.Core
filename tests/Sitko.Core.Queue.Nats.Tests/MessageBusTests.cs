using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Configuration;
using Sitko.Core.App;
using Sitko.Core.MediatR;
using Sitko.Core.Queue.Tests;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Queue.Nats.Tests;

public class MessageBusTests : BaseTest<NatsMessageBusTestScope>
{
    public MessageBusTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task TranslateNotification()
    {
        var scope = await GetScopeAsync();

        var queue = scope.GetService<IQueue>();

        Guid? receivedId = null;
        var sub = await queue.SubscribeAsync<TestRequest>((testRequest, _) =>
        {
            receivedId = testRequest.Id;
            return Task.FromResult(true);
        });
        Assert.True(sub.IsSuccess);

        var mediator = scope.GetService<IMediator>();
        var request = new TestRequest();
        await mediator.Publish(request);

        await Task.Delay(TimeSpan.FromSeconds(1));

        Assert.NotNull(receivedId);
        Assert.Equal(request.Id, receivedId);
    }
}

public class NatsMessageBusTestScope : NatsQueueTestScope
{
    protected override void ConfigureQueue(NatsQueueModuleOptions options, IConfiguration configuration,
        IAppEnvironment environment)
    {
        base.ConfigureQueue(options, configuration, environment);
        options.TranslateMediatRNotification<TestRequest>();
    }

    protected override TestApplication ConfigureApplication(TestApplication application, string name)
    {
        base.ConfigureApplication(application, name);
        application.AddMediatR<MessageBusTests>();
        return application;
    }
}

public class TestRequest : TestMessage, INotification
{
}
