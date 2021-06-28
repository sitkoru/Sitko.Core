using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sitko.Core.App.Collections;
using Sitko.Core.Storage;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public partial class AntStorageFilesInput<TCollection> where TCollection : ICollection<StorageItem>, new()
    {
        [Parameter] public Func<IEnumerable<StorageItem>, Task> OnUpdate { get; set; } = null!;
        [Parameter] public string UpText { get; set; } = "Move up";
        [Parameter] public string DownText { get; set; } = "Move down";

        private readonly OrderedCollection<UploadedFile> _files = new();
        protected override int ItemsCount => _files.Count();

        private TCollection Items
        {
            get
            {
                return CurrentValue;
            }
            set
            {
                _files.AddItems(value.Select(CreateUploadedItem));
                UpdateCurrentValue();
            }
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            if (CurrentValue is not null && CurrentValue.Any())
            {
                _files.SetItems(CurrentValue.Select(CreateUploadedItem));
            }
        }

        protected override void DoRemoveFile(UploadedFile file)
        {
            _files.RemoveItem(file);
        }

        protected override TCollection GetValue()
        {
            var collection = new TCollection();
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
