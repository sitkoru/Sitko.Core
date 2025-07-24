using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.App.Tests;

public class LoggingTests : BaseTest<LoggingTestScope>
{
    public LoggingTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Log()
    {
        var scope = await GetScopeAsync();
        var logger = scope.GetLogger<LoggingTests>();
        using (LogContext.PushProperty("Test", "123"))
        {
            logger.LogInformation("Test");
        }
    }
}

[UsedImplicitly]
public class LoggingTestScope : BaseTestScope;
