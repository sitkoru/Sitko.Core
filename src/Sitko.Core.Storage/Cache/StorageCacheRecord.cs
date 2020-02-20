namespace Sitko.Core.Storage.Cache
{
    public class StorageCacheRecord
    {
        protected StorageCacheRecord(StorageItem item)
        {
            Item = item;
        }

        public StorageItem Item { get; }
    }
}
