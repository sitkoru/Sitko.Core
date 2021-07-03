using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Sitko.Core.Storage;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public class AntStorageImagesInput<TValue> : BaseAntMultipleStorageInput<TValue>
        where TValue : class, ICollection<StorageItem>, new()
    {
        public override AntStorageInputMode Mode { get; set; } = AntStorageInputMode.Image;
        [Parameter] public override string ContentTypes { get; set; } = "image/jpeg,image/png,image/svg+xml";
    }
}
