using Sitko.Core.Storage;
using Sitko.Core.Storage.Remote;

namespace Sitko.Core.Apps.Blazor.Client;

public class TestRemoteStorageOptions : StorageOptions, IRemoteStorageOptions
{
    public Uri RemoteUrl { get; set; } = new("https://localhost");
    public Func<HttpClient>? HttpClientFactory { get; set; }
}
