using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.App.Json;

namespace Sitko.Core.IdProvider.SonyFlake
{
    public class SonyFlakeIdProvider : IIdProvider
    {
        private readonly IOptionsMonitor<SonyFlakeIdProviderModuleOptions> _optionsMonitor;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SonyFlakeIdProvider> _logger;
        private HttpClient _httpClient;

        public SonyFlakeIdProvider(IOptionsMonitor<SonyFlakeIdProviderModuleOptions> optionsMonitor,
            IHttpClientFactory httpClientFactory, ILogger<SonyFlakeIdProvider> logger)
        {
            _optionsMonitor = optionsMonitor;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _httpClient = CreateHttpClient();
            _optionsMonitor.OnChange(_ =>
            {
                _httpClient = CreateHttpClient();
            });
        }

        private HttpClient CreateHttpClient()
        {
            var httpClient = _httpClientFactory.CreateClient(nameof(SonyFlakeIdProvider));
            httpClient.BaseAddress = new Uri(_optionsMonitor.CurrentValue.Uri);
            return httpClient;
        }

        public async Task<long> NextAsync()
        {
            try
            {
                var response = await _httpClient.GetJsonAsync<SonyFlakeResponse>("/");
                if (response is not null)
                {
                    return response.Id;
                }

                _logger.LogError("Empty response from SonyFlake");
                throw new IdGenerationException();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while requesting SonyFlake: {ErrorText}", ex.ToString());
                throw new IdGenerationException();
            }
        }
    }
}
