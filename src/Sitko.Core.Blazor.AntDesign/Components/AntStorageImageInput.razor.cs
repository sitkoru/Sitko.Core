using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sitko.Core.Storage;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public partial class AntStorageImageInput
    {
        private UploadedImage? Image => CurrentValue is null ? null : CreateUploadedItem(CurrentValue);

        [Parameter] public Func<StorageItem?, Task> OnUpdate { get; set; } = null!;
        protected override int ItemsCount => 1;

        protected override void DoRemoveFile(UploadedImage file)
        {
            CurrentValue = null;
        }

        protected override StorageItem? GetValue()
        {
            return Image?.StorageItem;
        }
    }
}
