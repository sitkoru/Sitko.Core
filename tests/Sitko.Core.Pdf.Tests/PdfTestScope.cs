using Microsoft.Extensions.Hosting;
using Sitko.Core.Xunit;

namespace Sitko.Core.Pdf.Tests;

public class PdfTestScope : BaseTestScope
{
    protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name) =>
        base.ConfigureApplication(hostBuilder, name)
            .AddPdfRenderer(static options =>
            {
                options.BrowserWsEndpoint = Environment.GetEnvironmentVariable("PDF_TEST_BROWSER_WS_ENDPOINT");
                options.ExecutablePath = Environment.GetEnvironmentVariable("PDF_TEST_BROWSER_EXECUTABLE_PATH");
            });
}
