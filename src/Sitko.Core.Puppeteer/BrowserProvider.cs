using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PuppeteerSharp;

namespace Sitko.Core.Puppeteer;

public class BrowserProvider(
    IOptionsMonitor<PuppeteerModuleOptions> optionsMonitor,
    ILoggerFactory loggerFactory,
    ILogger<BrowserProvider> logger)
    : IBrowserProvider
{
    private PuppeteerModuleOptions Options => optionsMonitor.CurrentValue;

    public async Task<IBrowser> GetBrowserAsync()
    {
        if (!string.IsNullOrEmpty(Options.BrowserWsEndpoint))
        {
            logger.LogInformation("Connect to ws endpoint");
            return await PuppeteerSharp.Puppeteer.ConnectAsync(
                new ConnectOptions
                {
                    BrowserWSEndpoint = Options.BrowserWsEndpoint,
                    AcceptInsecureCerts = Options.AcceptInsecureCerts,
                    DefaultViewport = Options.ViewPortOptions
                },
                loggerFactory);
        }

        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH")))
        {
            logger.LogDebug("Download browser");
            var browserFetcher = new BrowserFetcher(Options.Product);
            if (!string.IsNullOrEmpty(Options.Revision))
            {
                await browserFetcher.DownloadAsync(Options.Revision);
            }
            else
            {
                await browserFetcher.DownloadAsync();
            }

            logger.LogDebug("Browser downloaded");
        }

        logger.LogInformation("Start new Browser");
        return await PuppeteerSharp.Puppeteer.LaunchAsync(
            new LaunchOptions
            {
                Headless = Options.Headless,
                Args = Options.BrowserArgs,
                AcceptInsecureCerts = Options.AcceptInsecureCerts,
                DefaultViewport = Options.ViewPortOptions
            }, loggerFactory);
    }
}
