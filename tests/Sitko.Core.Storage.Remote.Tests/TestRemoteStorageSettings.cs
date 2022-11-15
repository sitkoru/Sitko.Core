namespace Sitko.Core.Storage.Remote.Tests;

public class TestRemoteStorageSettings : StorageOptions, IRemoteStorageOptions
{
    public Uri RemoteUrl { get; set; } = new("http://localhost");
    public Func<HttpClient>? HttpClientFactory { get; set; }
}

