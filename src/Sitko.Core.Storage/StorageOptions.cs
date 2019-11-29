using System;

namespace Sitko.Core.Storage
{
    public class StorageOptions : IStorageOptions
    {
        public Uri PublicUri { get; set; }

        public bool ProcessImages { get; set; }

        public int LargeThumbnailWidth { get; set; } = 800;
        public int LargeThumbnailHeight { get; set; } = 800;
        public int MediumThumbnailWidth { get; set; } = 300;
        public int MediumThumbnailHeight { get; set; } = 300;
        public int SmallThumbnailWidth { get; set; } = 100;
        public int SmallThumbnailHeight { get; set; } = 100;
    }
}
