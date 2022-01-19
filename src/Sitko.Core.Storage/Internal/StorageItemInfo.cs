using System;

namespace Sitko.Core.Storage.Internal;

public record StorageItemInfo(string Path, long FileSize, DateTimeOffset Date);
