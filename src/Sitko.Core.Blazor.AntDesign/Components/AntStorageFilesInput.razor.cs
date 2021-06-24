using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sitko.Core.App.Collections;
using Sitko.Core.Storage;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public partial class AntStorageFilesInput
    {
        [Parameter] public Func<IEnumerable<StorageItem>, Task> OnUpdate { get; set; } = null!;
        [Parameter] public string UpText { get; set; } = "Move up";
        [Parameter] public string DownText { get; set; } = "Move down";
        [Parameter] public bool EnableOrdering { get; set; } = true;
        private readonly OrderedCollection<UploadedFile> _files = new();
        

        [Parameter]
        public IEnumerable<StorageItem>? InitialFiles
        {
            get
            {
                return Array.Empty<StorageItem>();
            }
            set
            {
                if (value is not null)
                {
                    _files.SetItems(value.Select(CreateUploadedItem));
                }
            }
        }


        protected override void AddFiles(IEnumerable<UploadedFile> items)
        {
            _files.AddItems(items);
        }

        protected override void RemoveFile(UploadedFile file)
        {
            _files.RemoveItem(file);
        }

        protected override Task UpdateStorageItems()
        {
            return OnUpdate(_files.OrderBy(i => i.Position).Select(file => file.StorageItem));
        }

        private bool CanMoveUp(UploadedFile file)
        {
            return _files.CanMoveUp(file);
        }

        private bool CanMoveDown(UploadedFile file)
        {
            return _files.CanMoveDown(file);
        }

        private Task MoveUpAsync(UploadedFile file)
        {
            _files.MoveUp(file);
            return UpdateFilesAsync();
        }

        private Task MoveDownAsync(UploadedFile file)
        {
            _files.MoveDown(file);
            return UpdateFilesAsync();
        }
    }
}
