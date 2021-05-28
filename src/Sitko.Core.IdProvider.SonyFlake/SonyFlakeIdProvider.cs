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
        private readonly IOptionsMonitor<SonyFlakeModuleOptions> _optionsMonitor;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SonyFlakeIdProvider> _logger;
        private HttpClient _httpClient;

        public SonyFlakeIdProvider(IOptionsMonitor<SonyFlakeModuleOptions> optionsMonitor,
            IHttpClientFactory httpClientFactory, ILogger<SonyFlakeIdProvider> logger)
        {
            _optionsMonitor = optionsMonitor;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            CreateHttpClient();
            _optionsMonitor.OnChange(options =>
            {
                CreateHttpClient();
            });
        }

        private void CreateHttpClient()
        {
            _httpClient = _httpClientFactory.CreateClient(nameof(SonyFlakeIdProvider));
            _httpClient.BaseAddress = new Uri(_optionsMonitor.CurrentValue.Uri);
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
