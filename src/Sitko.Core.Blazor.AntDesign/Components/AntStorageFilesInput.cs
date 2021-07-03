using System.Collections.Generic;
using Sitko.Core.Storage;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public class AntStorageFilesInput<TValue> : BaseAntMultipleStorageInput<TValue>
        where TValue : class, ICollection<StorageItem>, new()
    {
        public override AntStorageInputMode Mode { get; set; } = AntStorageInputMode.File;
    }
}
