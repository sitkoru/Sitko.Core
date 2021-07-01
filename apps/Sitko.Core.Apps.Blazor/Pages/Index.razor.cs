using System;
using System.Linq;
using System.Threading.Tasks;
using AntDesign;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Sitko.Core.App.Localization;
using Sitko.Core.Apps.Blazor.Components;
using Sitko.Core.Apps.Blazor.Data;
using Sitko.Core.Apps.Blazor.Data.Entities;
using Sitko.Core.Apps.Blazor.Data.Repositories;
using Sitko.Core.Apps.Blazor.Forms;
using Sitko.Core.Blazor.AntDesignComponents.Components;
using Sitko.Core.Storage;

namespace Sitko.Core.Apps.Blazor.Pages
{
    public partial class Index
    {
        private BarAntRepositoryList _barList = null!;
        private TableFilter<string>[] _barFilter = Array.Empty<TableFilter<string>>();
        private AntRepositoryForm<BarModel, Guid, BarForm> _frm = null!;
        private BarModel[] _bars = Array.Empty<BarModel>();
        [Inject] public IStorage<TestBlazorStorageOptions> Storage { get; set; } = null!;
        [Inject] public ILocalizationProvider<App> LocalizationProvider { get; set; } = null!;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            var bars = await GetService<BarRepository>().GetAllAsync();
            Bars = bars.items;
            _barFilter = (await GetService<BarContext>().Bars.Select(a => a.Bar).Distinct().ToListAsync())
                .Select(x => new TableFilter<string> { Text = x, Value = x }).ToArray();
            MarkAsInitialized();
        }

        public BarModel[] Bars
        {
            get => _bars;
            set => _bars = value;
        }

        private Task<object> GenerateMetadataAsync()
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
