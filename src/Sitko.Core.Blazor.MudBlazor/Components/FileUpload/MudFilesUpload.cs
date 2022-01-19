using System.Collections.Generic;
using System.Linq;
using Sitko.Core.Blazor.FileUpload;
using Sitko.Core.Storage;

namespace Sitko.Core.Blazor.MudBlazorComponents;

public class MudFilesUpload<TCollection> : MudFileUpload<TCollection>
    where TCollection : class, ICollection<StorageItem>, new()
{
    protected override bool IsMultiple => true;
    protected override IEnumerable<UploadedItem> GetFiles(TCollection value) => value.Select(CreateUploadedItem);

    protected override TCollection GetValue()
    {
        var collection = new TCollection();
        foreach (var file in Files)
        {
            collection.Add(file.StorageItem);
        }

        return collection;
    }
}
