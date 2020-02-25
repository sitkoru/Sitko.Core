namespace Sitko.Core.Storage
{
    public class StorageItem
    {
        public StorageItem()
        {
        }

        public StorageItem(StorageItem item)
        {
            FileName = item.FileName;
            FileSize = item.FileSize;
            FilePath = item.FilePath;
            Path = item.Path;
        }

        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string FilePath { get; set; }
        public string Path { get; set; }
        public string StorageFileName => FilePath.Substring(FilePath.LastIndexOf('/') + 1);

        public string HumanSize
        {
            get
            {
                return Helpers.HumanSize(FileSize);
            }
        }
    }
}
