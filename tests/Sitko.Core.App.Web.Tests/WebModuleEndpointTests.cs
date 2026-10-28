using Sitko.Core.Xunit;
using Sitko.Core.Xunit.Web;
using Xunit;

namespace Sitko.Core.App.Web.Tests
{

    public class WebModuleEndpointTests(ITestOutputHelper testOutputHelper) : BaseTest<WebModuleEndpointScope>(testOutputHelper)
    {
        [Fact]
        public async Task RegisteredWebModuleEndpointReturnsFixedResponse()
        {
            var scope = await GetScopeAsync();
            using var client = scope.CreateClient();

            var response = await client.GetAsync("/app-module", TestContext.Current.CancellationToken);

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("app-module-ok", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }
    }

    public class WebModuleEndpointScope : WebTestScope
    {
        protected override WebApplicationBuilder ConfigureWebApplication(WebApplicationBuilder webApplicationBuilder,
            string name)
        {
            var builder = base.ConfigureWebApplication(webApplicationBuilder, name);
            _ = builder.AddSitkoCoreWeb().AddModule<EndpointModule, EndpointModuleOptions>();
            return builder;
        }

        public HttpClient CreateClient() => Server!.CreateClient();
    }

    public class EndpointModuleOptions : BaseModuleOptions;

    public class EndpointModule : BaseApplicationModule<EndpointModuleOptions>, IWebApplicationModule<EndpointModuleOptions>
    {
        public override string OptionsKey => "EndpointModuleTest";

        public void ConfigureEndpoints(IApplicationContext applicationContext, IApplicationBuilder appBuilder,
            IEndpointRouteBuilder endpoints) => _ = endpoints.MapGet("/app-module", static () => "app-module-ok");
    }
}
