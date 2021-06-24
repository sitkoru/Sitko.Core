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
        [Parameter] public Func<StorageItem?, Task> OnUpdate { get; set; } = null!;
        private UploadedImage? _image;
        protected override int ItemsCount => 1;

        [Parameter]
        public StorageItem? InitialImage
        {
            get
            {
                return null;
            }
            set
            {
                if (value is not null)
                {
                    _image = CreateUploadedItem(value);
                }
            }
        }

        protected override void AddFiles(IEnumerable<UploadedImage> items)
        {
            _image = items.First();
        }

        protected override void RemoveFile(UploadedImage file)
        {
            _image = null;
        }

        protected override Task UpdateStorageItems()
        {
            return OnUpdate(_image?.StorageItem);
        }
    }
}
