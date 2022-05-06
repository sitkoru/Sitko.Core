using System;
using System.Net.Http;

namespace Sitko.Core.Storage.Remote.Tests;

public class TestRemoteStorageSettings : StorageOptions, IRemoteStorageOptions
{
    public Uri RemoteUrl { get; set; }
    public Func<HttpClient>? HttpClientFactory { get; set; }
}
