namespace Sitko.Core.Storage.Cache
{
    public class InMemoryStorageCacheOptions : StorageCacheOptions
    {
        public long MaxFileSizeToStore { get; set; }
        public long? MaxCacheSize { get; set; }
    }
}
