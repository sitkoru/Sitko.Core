using System.Collections.Generic;
using Sitko.Core.Blazor.FileUpload;
using Sitko.Core.Storage;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public partial class AntStorageItemsInput<TValue> where TValue : ICollection<StorageItem>, new()
    {
        private bool IsMultiple => MaxAllowedFiles is null || MaxAllowedFiles > 1;


        protected override TValue GetResult(IEnumerable<StorageFileUploadResult> results)
        {
            var collection = new TValue();
            foreach (var result in results)
            {
                collection.Add(result.StorageItem);
            }

            return collection;
        }
    }

    public class ListAntStorageItemsInput : AntStorageItemsInput<List<StorageItem>>
    {
    }
}
