using Sitko.Core.Storage;
using Sitko.Core.Storage.Remote;

namespace Sitko.Core.Apps.Blazor.Client;

public class RemoteStorageOptions : StorageOptions, IRemoteStorageOptions
{
    public Uri RemoteUrl { get; set; }
    public Func<HttpClient>? HttpClientFactory { get; set; }
}
