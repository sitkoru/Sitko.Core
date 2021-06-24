using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sitko.Core.Storage;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public partial class AntStorageFilesInput
    {
        [Parameter] public Func<IEnumerable<StorageItem>, Task> OnUpdate { get; set; } = null!;
        private List<UploadedFile> _files = new();

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
                    _files = value.Select(CreateUploadedItem).ToList();
                }
            }
        }

        protected override void AddFiles(IEnumerable<UploadedFile> items)
        {
            _files.AddRange(items);
        }

        protected override void RemoveFile(UploadedFile file)
        {
            _files.Remove(file);
        }

        protected override Task UpdateStorageItems()
        {
            return OnUpdate(_files.Select(file => file.StorageItem));
        }
    }
}
