using MediatR;
using Sitko.Core.App;
using Sitko.Core.MediatR;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Queue.Tests;

public class MessageBusTests : BaseTestQueueTest<MessageBusTestScope>
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

public class MessageBusTestScope : BaseTestQueueTestScope
{
    protected override TestApplication ConfigureApplication(TestApplication application, string name)
    {
        base.ConfigureApplication(application, name);
        application.AddMediatR<MessageBusTests>();
        return application;
    }

    protected override void Configure(IApplicationContext applicationContext,
        TestQueueOptions options, string name) =>
        options.TranslateMediatRNotification<TestRequest>();
}

public class TestRequest : TestMessage, INotification
{
}

