using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sitko.Core.App.Collections;
using Sitko.Core.Storage;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public partial class AntStorageFilesInput<TValue> where TValue : ICollection<StorageItem>, new()
    {
        [Parameter] public Func<IEnumerable<StorageItem>, Task> OnUpdate { get; set; } = null!;
        [Parameter] public string UpText { get; set; } = "";
        [Parameter] public string DownText { get; set; } = "";

        private readonly OrderedCollection<UploadedFile> _files = new();
        protected override int ItemsCount => _files.Count();

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
                    _files.AddItems(value.Select(CreateUploadedItem));
                }
                else
                {
                    _files.SetItems(Array.Empty<UploadedFile>());
                }

                UpdateCurrentValue();
            }
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            if (string.IsNullOrEmpty(UpText))
            {
                UpText = LocalizationProvider["Move up"];
            }

            if (string.IsNullOrEmpty(DownText))
            {
                DownText = LocalizationProvider["Move down"];
            }

            if (CurrentValue is not null && CurrentValue.Any())
            {
                _files.SetItems(CurrentValue.Select(CreateUploadedItem));
            }
        }

        protected override void DoRemoveFile(UploadedFile file)
        {
            _files.RemoveItem(file);
        }

        protected override TValue GetValue()
        {
            var collection = new TValue();
            foreach (var result in _files)
            {
                collection.Add(result.StorageItem);
            }

            return collection;
        }

        private bool CanMoveUp(UploadedFile file)
        {
            return _files.CanMoveUp(file);
        }

        private bool CanMoveDown(UploadedFile file)
        {
            return _files.CanMoveDown(file);
        }

        private void MoveUp(UploadedFile file)
        {
            _files.MoveUp(file);
            UpdateCurrentValue();
        }

        private void MoveDown(UploadedFile file)
        {
            _files.MoveDown(file);
            UpdateCurrentValue();
        }
    }
}
