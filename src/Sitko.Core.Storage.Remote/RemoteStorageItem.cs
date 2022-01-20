using System;

namespace Sitko.Core.Storage.Remote;

public record RemoteStorageItem(StorageItem StorageItem, Uri PublicUri);
