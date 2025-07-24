using Sitko.Core.App;
using Sitko.Core.Queue.Tests;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.Queue.Nats.Tests;

public class OptionsTest : BaseTest<NatsQueueTestScopeWithOptions>
{
    public OptionsTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task CheckOptions()
    {
        var scope = await GetScopeAsync();

        var queue = scope.GetService<IQueue>();
        var messageOptions = scope.GetService<IEnumerable<IQueueMessageOptions>>().ToArray();
        Assert.NotNull(messageOptions);
        Assert.NotEmpty(messageOptions);

        var testMessageOptions = messageOptions.FirstOrDefault(o => o is IQueueMessageOptions<TestMessage>);
        Assert.NotNull(testMessageOptions);

        var subResult = await queue.SubscribeAsync<TestMessage>((_, _) => Task.FromResult(true));
        Assert.True(subResult.IsSuccess);

        Assert.NotNull(subResult.Options);

        Assert.Equal(testMessageOptions, subResult.Options);
    }
}

public class NatsQueueTestScopeWithOptions : NatsQueueTestScope
{
    protected override void ConfigureQueue(NatsQueueModuleOptions options, IApplicationContext applicationContext)
    {
        base.ConfigureQueue(options, applicationContext);
        options.ConfigureMessage(new NatsMessageOptions<TestMessage>
        {
            StartAt = TimeSpan.FromMinutes(30), ManualAck = true
        });
    }
}
