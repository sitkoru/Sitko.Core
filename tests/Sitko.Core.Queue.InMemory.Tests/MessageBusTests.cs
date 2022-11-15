using MediatR;
using Sitko.Core.App;
using Sitko.Core.MediatR;
using Sitko.Core.Queue.Tests;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Queue.InMemory.Tests;

public class MessageBusTests : BaseTest<InMemoryMessageBusTestScope>
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

public class InMemoryMessageBusTestScope : InMemoryQueueTestScope
{
    protected override void Configure(IApplicationContext applicationContext,
        InMemoryQueueModuleOptions options,
        string name)
    {
        base.Configure(applicationContext, options, name);
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

