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
        private BarAntRepositoryList barList = null!;
        private TableFilter<string>[] barFilter = Array.Empty<TableFilter<string>>();
        private AntRepositoryForm<BarModel, Guid, BarForm> frm = null!;
        private BarModel[] bars = Array.Empty<BarModel>();
        public BarModel? Bar { get; set; }
        [Inject] public IStorage Storage { get; set; } = null!;
        [Inject] public ILocalizationProvider<App> LocalizationProvider { get; set; } = null!;
        private decimal Summary { get; set; }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            var result = await GetService<BarRepository>().GetAllAsync();
            if (result.itemsCount == 0)
            {
                await GetService<BarRepository>().AddAsync(new BarModel {Bar = "Bar", Id = Guid.NewGuid()});
                result = await GetService<BarRepository>().GetAllAsync();
            }

            Bars = result.items;
            barFilter = (await GetService<BarContext>().Bars.Select(a => a.Bar).Distinct().ToListAsync())
                .Select(x => new TableFilter<string> {Text = x, Value = x}).ToArray();
            Bar = Bars.First();
        }

        public BarModel[] Bars
        {
            get => bars;
            set => bars = value;
        }

        private static Task<object> GenerateMetadataAsync()
        {
            var metadata = new BarStorageMetadata();
            return Task.FromResult<object>(metadata);
        }


        private static Task InitFormModelAsync(BarForm form)
        {
            form.Test = Guid.NewGuid();
            return Task.CompletedTask;
        }

        private async Task CountSummaryAsync() => Summary = await barList.SumAsync(model => model.Sum);
    }

    public class BarStorageMetadata
    {
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
