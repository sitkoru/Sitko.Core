using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Sitko.Core.Storage.Remote.Server;

public class UploadStorageItem<TMetadata>
{
    public IFormFile? File { get; set; }
    public string? Path { get; set; }

    [ModelBinder(BinderType = typeof(UploadStorageItemBinder))]
    public TMetadata? Metadata { get; set; }

    public string? FileName { get; set; }
}
