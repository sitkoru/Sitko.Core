using System.Collections.Generic;
using Sitko.Core.App.Collections;
using Sitko.Core.Storage;

namespace Sitko.Core.Blazor.FileUpload;

public class UploadedItem : IOrdered
{
    public UploadedItem(StorageItem storageItem, Dictionary<UploadedItemUrlType, string> urls)
    {
        StorageItem = storageItem;
        Urls = urls;
    }

    private Dictionary<UploadedItemUrlType, string> Urls { get; }
    public string SmallPreviewUrl => GetUrl(UploadedItemUrlType.SmallPreview);
    public string LargePreviewUrl => GetUrl(UploadedItemUrlType.LargePreview);
    public string Url => GetUrl(UploadedItemUrlType.Full);

    public StorageItem StorageItem { get; }

    public int Position { get; set; }

    private string GetUrl(UploadedItemUrlType type)
    {
        if (Urls.ContainsKey(type))
        {
            return Urls[type];
        }

        return Urls[UploadedItemUrlType.Full];
    }
}

public enum UploadedItemUrlType
{
    Full,
    SmallPreview,
    LargePreview
}
