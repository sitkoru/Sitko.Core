namespace Sitko.Core.Storage.Cache
{
    public abstract class StorageCacheRecord
    {
        protected StorageCacheRecord(StorageItem item)
        {
            Item = item;
        }

        public StorageItem Item { get; }
    }
}
