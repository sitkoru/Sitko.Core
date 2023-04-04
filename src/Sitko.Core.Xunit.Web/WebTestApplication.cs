using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Sitko.Core.App;
using Sitko.Core.App.Web;

namespace Sitko.Core.Xunit.Web;

public abstract class WebTestApplication<TStartup> : WebApplication<TStartup> where TStartup : TestStartup
{
    protected WebTestApplication(string[] args) : base(args)
    {
    }

    protected override void ConfigureWebHostDefaults(IApplicationContext applicationContext,
        IWebHostBuilder webHostBuilder)
    {
        base.ConfigureWebHostDefaults(applicationContext, webHostBuilder);
        webHostBuilder.UseTestServer();
    }
}

public class WebTestApplication : WebTestApplication<TestStartup>
{
    public WebTestApplication(string[] args) : base(args)
    {
    }
}
