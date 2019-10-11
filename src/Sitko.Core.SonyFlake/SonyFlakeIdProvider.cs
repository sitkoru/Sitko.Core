using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sitko.Core.Infrastructure.Json;

namespace Sitko.Core.SonyFlake
{
    public class SonyFlakeIdProvider : IIdProvider
    {
        private readonly ILogger<SonyFlakeIdProvider> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SonyFlakeModuleConfig _config;

        public SonyFlakeIdProvider(SonyFlakeModuleConfig config,
            ILogger<SonyFlakeIdProvider> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        public async Task<long> NextAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetJsonAsync<SonyFlakeResponse>(_config.SonyflakeUri.ToString());
                return response.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw new IdGenerationException();
            }
        }
    }
}
