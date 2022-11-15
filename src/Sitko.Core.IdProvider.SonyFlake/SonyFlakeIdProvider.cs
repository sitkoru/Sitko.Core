using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.App.Json;

namespace Sitko.Core.IdProvider.SonyFlake;

public class SonyFlakeIdProvider : IIdProvider
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<SonyFlakeIdProvider> logger;
    private readonly IOptionsMonitor<SonyFlakeIdProviderModuleOptions> optionsMonitor;
    private HttpClient httpClient;

    public SonyFlakeIdProvider(IOptionsMonitor<SonyFlakeIdProviderModuleOptions> optionsMonitor,
        IHttpClientFactory httpClientFactory, ILogger<SonyFlakeIdProvider> logger)
    {
        this.optionsMonitor = optionsMonitor;
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;
        httpClient = CreateHttpClient();
        this.optionsMonitor.OnChange(_ =>
        {
            httpClient = CreateHttpClient();
        });
    }

    public async Task<long> NextAsync()
    {
        try
        {
            var response = await httpClient.GetJsonAsync<SonyFlakeResponse>("/");
            if (response is not null)
            {
                return response.Id;
            }

            logger.LogError("Empty response from SonyFlake");
            throw new IdGenerationException();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while requesting SonyFlake: {ErrorText}", ex.ToString());
            throw new IdGenerationException();
        }
    }

    private HttpClient CreateHttpClient()
    {
        var client = httpClientFactory.CreateClient(nameof(SonyFlakeIdProvider));
        client.BaseAddress = new Uri(optionsMonitor.CurrentValue.Uri);
        return client;
    }
}

