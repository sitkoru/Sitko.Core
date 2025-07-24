using Microsoft.Extensions.Hosting;
using Sitko.Core.Email.Tests;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.Email.Smtp.Tests;

public class BasicTests : BasicTests<SmtpTestsScope>
{
    public BasicTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }
}

public class SmtpTestsScope : BaseTestScope
{
    protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name) =>
        base.ConfigureApplication(hostBuilder, name).AddSmtpEmail();
}
