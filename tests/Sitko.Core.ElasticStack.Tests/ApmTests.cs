using Elastic.Apm;
using Elastic.Apm.Api;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.ElasticStack.Tests;

public class ApmTests : BaseTest
{
    public ApmTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Transaction()
    {
        var scope = await GetScopeAsync<ElasticStackScope>();
        await scope.StartApplicationAsync(TestContext.Current.CancellationToken);
        Assert.True(Agent.IsConfigured, "APM is not configured");
        var tracer = scope.GetService<ITracer>();
        Assert.NotNull(tracer);
        for (var i = 0; i < 100; i++)
        {
            var transaction = Agent
                .Tracer.StartTransaction("MyTransaction", ApiConstants.TypeRequest);
            try
            {
                if (i % 8 == 0)
                {
                    throw new InvalidOperationException("Transaction exception");
                }
            }
            catch (Exception e)
            {
                transaction.CaptureException(e);
            }
            finally
            {
                transaction.End();
            }
        }

        await Task.Delay(TimeSpan.FromSeconds(5));
        Assert.True(true);
    }
}
