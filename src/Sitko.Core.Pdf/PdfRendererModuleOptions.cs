using Microsoft.Playwright;
using Sitko.Core.App;

namespace Sitko.Core.Pdf
{
    public class PdfRendererModuleOptions : BaseModuleOptions
    {
        public string? BrowserWsEndpoint { get; set; }
        public string? ExecutablePath { get; set; }
        public bool AcceptInsecureCerts { get; set; }
        public ViewportSize ViewportSize { get; set; } = new() { Width = 800, Height = 600 };
        public string[] BrowserArgs { get; set; } = ["--no-sandbox"];
        public bool Headless { get; set; } = true;
    }
}
