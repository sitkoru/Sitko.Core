using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sitko.Core.Storage;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public partial class AntStorageImagesInput
    {
        [Parameter] public Func<IEnumerable<StorageItem>, Task> OnUpdate { get; set; } = null!;
        private List<UploadedImage> _images = new();

        [Parameter]
        public IEnumerable<StorageItem>? InitialImages
        {
            get
            {
                return Array.Empty<StorageItem>();
            }
            set
            {
                if (value is not null)
                {
                    _images = value.Select(CreateUploadedItem).ToList();
                }
            }
        }

        protected override void AddFiles(IEnumerable<UploadedImage> items)
        {
            _images.AddRange(items);
        }

        protected override void RemoveFile(UploadedImage file)
        {
            _images.Remove(file);
        }

        protected override Task UpdateStorageItems()
        {
            return OnUpdate(_images.Select(image => image.StorageItem));
        }
    }
}
