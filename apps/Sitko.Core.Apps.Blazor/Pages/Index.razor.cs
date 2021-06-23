using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntDesign;
using Microsoft.EntityFrameworkCore;
using Sitko.Core.Apps.Blazor.Components;
using Sitko.Core.Apps.Blazor.Data;
using Sitko.Core.Apps.Blazor.Data.Entities;
using Sitko.Core.Apps.Blazor.Data.Repositories;
using Sitko.Core.Blazor.AntDesignComponents.Components;
using Sitko.Core.Blazor.FileUpload;

namespace Sitko.Core.Apps.Blazor.Pages
{
    public partial class Index
    {
        private BarAntRepositoryList _barList;
        private TableFilter<string>[] _barFilter;
        private AntRepositoryForm<BarModel, Guid, BarForm> _frm;

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

        public async Task FilesUploadedAsync(BarForm bar, IEnumerable<StorageFileUploadResult> results)
        {
            bar.StorageItem = results.FirstOrDefault()?.StorageItem;
            await NotifyStateChangeAsync();
        }

        private Task<object> GenerateMetadataAsync(FileUploadRequest request, FileStream stream)
        {
            var metadata = new BarStorageMetadata();
            return Task.FromResult<object>(metadata);
        }
    }

    public class BarStorageMetadata
    {
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
