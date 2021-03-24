using System;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.ElasticStack.Tests
{
    public class ApmTests : BaseTest
    {
        public ApmTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task Transaction()
        {
            var scope = await GetScopeAsync<ElasticStackScope>();
            await scope.StartApplicationAsync();
            Assert.True(Elastic.Apm.Agent.IsConfigured, "APM is not configured");
            var tracer = scope.Get<ITracer>();
            Assert.NotNull(tracer);
            for (var i = 0; i < 100; i++)
            {
                var transaction = Elastic.Apm.Agent
                    .Tracer.StartTransaction("MyTransaction", ApiConstants.TypeRequest);
                try
                {
                    if (i % 8 == 0)
                    {
                        throw new Exception("Transaction exception");
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
}
