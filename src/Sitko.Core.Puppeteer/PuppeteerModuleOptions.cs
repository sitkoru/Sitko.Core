using System.Text.Json.Serialization;
using PuppeteerSharp;
using Sitko.Core.App;

namespace Sitko.Core.Puppeteer;

public class PuppeteerModuleOptions : BaseModuleOptions
{
    public string? BrowserWsEndpoint { get; set; }
    public bool AcceptInsecureCerts { get; set; }

    [JsonIgnore] public ViewPortOptions ViewPortOptions { get; set; } = ViewPortOptions.Default;
    public string[] BrowserArgs { get; set; } = { "--no-sandbox" };
    public bool Headless { get; set; } = true;
    public SupportedBrowser Product { get; set; } = SupportedBrowser.Chrome;
    public string? Revision { get; set; }
}
