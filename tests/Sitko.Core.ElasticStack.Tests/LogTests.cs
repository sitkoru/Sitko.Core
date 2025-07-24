using Microsoft.Extensions.Logging;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.ElasticStack.Tests;

public class LogTests : BaseTest
{
    public LogTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Log()
    {
        var scope = await GetScopeAsync<ElasticStackScope>();
        var logger = scope.GetLogger<LogTests>();
        for (var i = 0; i < 100; i++)
        {
            logger.LogInformation("Test log: {Iteration}", i);
        }

        await Task.Delay(TimeSpan.FromSeconds(5));
        Assert.True(true);
    }
}
