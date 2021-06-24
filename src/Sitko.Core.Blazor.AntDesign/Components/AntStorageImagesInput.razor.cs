using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AntDesign;
using Microsoft.AspNetCore.Components;
using Sitko.Core.App.Collections;
using Sitko.Core.Storage;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public partial class AntStorageImagesInput
    {
        [Parameter] public Func<IEnumerable<StorageItem>, Task> OnUpdate { get; set; } = null!;
        [Parameter] public string LeftText { get; set; } = "Move left";
        [Parameter] public string RightText { get; set; } = "Move right";
        private readonly OrderedCollection<UploadedImage> _images = new();
        protected override int ItemsCount => _images.Count();

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
                    _images.SetItems(value.Select(CreateUploadedItem));
                }
            }
        }

        protected override void AddFiles(IEnumerable<UploadedImage> items)
        {
            _images.AddItems(items);
        }

        protected override void RemoveFile(UploadedImage file)
        {
            _images.RemoveItem(file);
        }

        protected override Task UpdateStorageItems()
        {
            return OnUpdate(_images.OrderBy(i => i.Position).Select(image => image.StorageItem));
        }

        private bool CanMoveLeft(UploadedImage image)
        {
            return _images.CanMoveUp(image);
        }

        private bool CanMoveRight(UploadedImage image)
        {
            return _images.CanMoveDown(image);
        }

        private Task MoveLeftAsync(UploadedImage image)
        {
            _images.MoveUp(image);
            return UpdateFilesAsync();
        }

        private Task MoveRightAsync(UploadedImage image)
        {
            _images.MoveDown(image);
            return UpdateFilesAsync();
        }
    }
}
