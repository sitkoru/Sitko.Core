using PuppeteerSharp;

namespace Sitko.Core.Puppeteer;

public interface IBrowserProvider
{
    Task<IBrowser> GetBrowserAsync();
}

