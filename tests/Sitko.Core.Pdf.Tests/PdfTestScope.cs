using Microsoft.Extensions.Hosting;
using Sitko.Core.Puppeteer;
using Sitko.Core.Xunit;

namespace Sitko.Core.Pdf.Tests;

public class PdfTestScope : BaseTestScope
{
    protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name) =>
        base.ConfigureApplication(hostBuilder, name)
            .AddPuppeteer()
            .AddPuppeteer();
}
