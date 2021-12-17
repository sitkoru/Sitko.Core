using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PuppeteerSharp;

namespace Sitko.Core.Puppeteer;

public class BrowserProvider : IBrowserProvider
{
    private readonly ILogger<BrowserProvider> logger;
    private readonly ILoggerFactory loggerFactory;
    private readonly IOptionsMonitor<PuppeteerModuleOptions> optionsMonitor;

    public BrowserProvider(IOptionsMonitor<PuppeteerModuleOptions> optionsMonitor, ILoggerFactory loggerFactory,
        ILogger<BrowserProvider> logger)
    {
        this.optionsMonitor = optionsMonitor;
        this.loggerFactory = loggerFactory;
        this.logger = logger;
    }

    private PuppeteerModuleOptions Options => optionsMonitor.CurrentValue;


    public async Task<Browser> GetBrowserAsync()
    {
        if (!string.IsNullOrEmpty(Options.BrowserWsEndpoint))
        {
            logger.LogInformation("Connect to ws endpoint");
            return await PuppeteerSharp.Puppeteer.ConnectAsync(
                new ConnectOptions
                {
                    BrowserWSEndpoint = Options.BrowserWsEndpoint,
                    IgnoreHTTPSErrors = Options.IgnoreHTTPSErrors,
                    DefaultViewport = Options.ViewPortOptions
                },
                loggerFactory);
        }

        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH")))
        {
            logger.LogDebug("Download browser");
            using var browserFetcher = new BrowserFetcher(Options.Product);
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
                IgnoreHTTPSErrors = Options.IgnoreHTTPSErrors,
                DefaultViewport = Options.ViewPortOptions
            }, loggerFactory);
    }
}
