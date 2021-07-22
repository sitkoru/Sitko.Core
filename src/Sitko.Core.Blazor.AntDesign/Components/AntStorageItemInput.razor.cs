using System.Collections.Generic;
using System.Linq;
using Sitko.Core.Blazor.FileUpload;
using Sitko.Core.Storage;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public partial class AntStorageItemInput
    {
        protected override StorageItem? GetResult(IEnumerable<StorageFileUploadResult> results) => results.FirstOrDefault()?.StorageItem;
    }
}
