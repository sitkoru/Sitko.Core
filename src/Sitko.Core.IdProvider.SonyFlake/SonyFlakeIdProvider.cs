using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sitko.Core.App.Json;

namespace Sitko.Core.IdProvider.SonyFlake
{
    public class SonyFlakeIdProvider : IIdProvider
    {
        private readonly ILogger<SonyFlakeIdProvider> _logger;
        private readonly HttpClient _httpClient;

        public SonyFlakeIdProvider(HttpClient httpClient, ILogger<SonyFlakeIdProvider> logger)
        {
            _logger = logger;
            _httpClient = httpClient;
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
