using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App.Web;
using Sitko.Core.Email.Tests;
using Sitko.Core.Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Email.Smtp.Tests
{
    public class BasicTests : BasicTests<SmtpTestsScope>
    {
        public BasicTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
    }

    public class SmtpTestsScope : BaseTestScope<TestApplication>
    {
    }
    
    public class TestStartup : BaseStartup
    {
        public TestStartup(IConfiguration configuration, IHostEnvironment environment) : base(
            configuration, environment)
        {
        }
    }
    
    public class TestApplication : WebApplication<TestStartup>
    {
        public TestApplication(string[] args) : base(args)
        {
            AddModule<SmtpEmailModule, SmtpEmailModuleConfig>((configuration, _, moduleConfig) =>
            {
                moduleConfig.Server = configuration["EMAIL_TESTS_SMTP_HOST"];
                moduleConfig.Port = int.Parse(configuration["EMAIL_TESTS_SMTP_PORT"]);
                moduleConfig.UserName = configuration["EMAIL_TESTS_SMTP_USERNAME"];
                moduleConfig.Password = configuration["EMAIL_TESTS_SMTP_PASSWORD"];
                moduleConfig.From = configuration["EMAIL_TESTS_FROM"];
                moduleConfig.Host = new HostString("tests.local");
                moduleConfig.Scheme = "https";
            });
        }
    }
}
