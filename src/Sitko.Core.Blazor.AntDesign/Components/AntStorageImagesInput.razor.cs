using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sitko.Core.App.Collections;
using Sitko.Core.Storage;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public partial class AntStorageImagesInput<TValue> where TValue : ICollection<StorageItem>, new()
    {
        [Parameter] public Func<IEnumerable<StorageItem>, Task> OnUpdate { get; set; } = null!;

        private readonly OrderedCollection<UploadedImage> _images = new();
        protected override int ItemsCount => _images.Count();

        private TValue? Items
        {
            get
            {
                return CurrentValue;
            }
            set
            {
                if (value is not null)
                {
                    _images.AddItems(value.Select(CreateUploadedItem));
                }
                else
                {
                    _images.SetItems(Array.Empty<UploadedImage>());
                }

                UpdateCurrentValue();
            }
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
           
            if (CurrentValue is not null && CurrentValue.Any())
            {
                _images.SetItems(CurrentValue.Select(CreateUploadedItem));
            }
        }

        protected override void DoRemoveFile(UploadedImage file)
        {
            _images.RemoveItem(file);
        }

        protected override TValue GetValue()
        {
            var collection = new TValue();
            foreach (var result in _images)
            {
                collection.Add(result.StorageItem);
            }

            return collection;
        }

        private bool CanMoveLeft(UploadedImage image)
        {
            return _images.CanMoveUp(image);
        }

        private bool CanMoveRight(UploadedImage image)
        {
            return _images.CanMoveDown(image);
        }

        private void MoveLeft(UploadedImage image)
        {
            _images.MoveUp(image);
            UpdateCurrentValue();
        }

        private void MoveRight(UploadedImage image)
        {
            _images.MoveDown(image);
            UpdateCurrentValue();
        }
    }
}
