using Sitko.Core.Storage;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public abstract class BaseAntStorageFileInput : BaseAntStorageInput<UploadedFile>
    {
        protected override UploadedFile CreateUploadedItem(StorageItem storageItem)
        {
            return new(storageItem, Storage.PublicUri(storageItem).ToString());
        }
    }
    
    public class UploadedFile : UploadedItem
    {
        public UploadedFile(StorageItem storageItem, string url) : base(storageItem)
        {
            Url = url;
        }

        public override string Url { get; }
    }
}
