using System;
using System.Collections.Generic;

namespace Sitko.Core.Storage
{
    public interface IStorageOptions
    {
        Uri PublicUri { get; }
        List<StorageImageSize> Thumbnails { get; }
    }
}
