using System;
using System.Linq;
using System.Threading.Tasks;
using AntDesign;
using Microsoft.EntityFrameworkCore;
using Sitko.Core.Apps.Blazor.Components;
using Sitko.Core.Apps.Blazor.Data;
using Sitko.Core.Apps.Blazor.Data.Entities;
using Sitko.Core.Apps.Blazor.Data.Repositories;
using Sitko.Core.Apps.Blazor.Forms;
using Sitko.Core.Storage;

namespace Sitko.Core.Apps.Blazor.Pages
{
    public partial class Index
    {
        private BarAntRepositoryList barList = null!;
        private TableFilter<string>[] barFilter = Array.Empty<TableFilter<string>>();
        private BarForm frm = null!;
        private BarModel[] bars = Array.Empty<BarModel>();
        public BarModel? Bar { get; set; }
        private IStorage Storage => GetRequiredService<IStorage>();

        public BarRepository ScopedBarRepository => GetRequiredService<BarRepository>();

        private decimal Summary { get; set; }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            var result = await GetRequiredService<BarRepository>().GetAllAsync();
            if (result.itemsCount == 0)
            {
                await GetRequiredService<BarRepository>().AddAsync(new BarModel { Bar = "Bar", Id = Guid.NewGuid() });
                result = await GetRequiredService<BarRepository>().GetAllAsync();
            }

            Bars = result.items;
            barFilter = (await GetRequiredService<BarContext>().Bars.Select(a => a.Bar).Distinct().ToListAsync())
                .Select(x => new TableFilter<string> { Text = x, Value = x }).ToArray();
            Bar = Bars.OrderBy(b => b.Id).First();
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

        private async Task CountSummaryAsync() => Summary = await barList.SumAsync(model => model.Sum);
    }

    public class BarStorageMetadata
    {
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
