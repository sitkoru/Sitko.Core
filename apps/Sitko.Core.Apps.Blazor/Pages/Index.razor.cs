using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntDesign;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Sitko.Core.Apps.Blazor.Components;
using Sitko.Core.Apps.Blazor.Data;
using Sitko.Core.Apps.Blazor.Data.Entities;
using Sitko.Core.Apps.Blazor.Data.Repositories;
using Sitko.Core.Apps.Blazor.Forms;
using Sitko.Core.Blazor.AntDesignComponents.Components;
using Sitko.Core.Blazor.FileUpload;
using Sitko.Core.Storage;

namespace Sitko.Core.Apps.Blazor.Pages
{
    public partial class Index
    {
        private BarAntRepositoryList _barList;
        private TableFilter<string>[] _barFilter;
        private AntRepositoryForm<BarModel, Guid, BarForm> _frm;
        [Inject] public IStorage<TestBlazorStorageOptions> Storage { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            // var bar = new BarModel() {Id = Guid.NewGuid(), Bar = "123"};
            // await GetService<BarRepository>().AddAsync(bar);
            var bars = await GetService<BarRepository>().GetAllAsync();
            Bars = bars.items;
            _barFilter = (await GetService<BarContext>().Bars.Select(a => a.Bar).Distinct().ToListAsync())
                .Select(x => new TableFilter<string> {Text = x, Value = x}).ToArray();
            MarkAsInitialized();
        }

        public BarModel[] Bars { get; set; }

        private Task<object> GenerateMetadataAsync(FileUploadRequest request, FileStream stream)
        {
            var metadata = new BarStorageMetadata();
            return Task.FromResult<object>(metadata);
        }

        private void PreviewFile(StorageItem file)
        {
            throw new NotImplementedException();
        }

        private void RemoveFile(StorageItem file)
        {
            throw new NotImplementedException();
        }
    }

    public class BarStorageMetadata
    {
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
