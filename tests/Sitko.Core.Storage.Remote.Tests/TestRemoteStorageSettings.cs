using System;

namespace Sitko.Core.Storage.Remote.Tests;

public class TestRemoteStorageSettings : StorageOptions, IRemoteStorageOptions
{
    public Uri RemoteUrl { get; set; }
}
