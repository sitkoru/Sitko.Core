using System.Text.Json;

namespace Sitko.Core.App.Json;

public static class HttpClientExtensions
{
    public static async Task<T?> GetJsonAsync<T>(this HttpClient client, string url,
        CancellationToken cancellationToken = default)
    {
        var response = await client.GetAsync(url, cancellationToken);
        return JsonSerializer.Deserialize<T>(await response.Content.ReadAsByteArrayAsync(cancellationToken));
    }
}

