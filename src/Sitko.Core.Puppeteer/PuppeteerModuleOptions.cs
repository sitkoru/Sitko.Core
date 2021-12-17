using System.Text.Json.Serialization;
using PuppeteerSharp;
using Sitko.Core.App;

namespace Sitko.Core.Puppeteer;

public class PuppeteerModuleOptions : BaseModuleOptions
{
    public string? BrowserWsEndpoint { get; set; }
    public bool IgnoreHTTPSErrors { get; set; }

    [JsonIgnore] public ViewPortOptions ViewPortOptions { get; set; } = ViewPortOptions.Default;
    public string[] BrowserArgs { get; set; } = { "--no-sandbox" };
    public bool Headless { get; set; } = true;
    public Product Product { get; set; } = Product.Chrome;
    public string? Revision { get; set; }
}
