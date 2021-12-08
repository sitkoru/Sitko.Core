using System;
using System.Threading.Tasks;
using Sitko.Core.Apps.MudBlazorDemo.Components;

namespace Sitko.Core.Apps.MudBlazorDemo.Pages
{
    public partial class Index
    {
        private BarRepositoryList barList = null!;
        private decimal Summary { get; set; }
        private async Task CountSummaryAsync() => Summary = await barList.SumAsync(model => model.Sum);
        //
        // private TableFilter<string>[] barFilter = Array.Empty<TableFilter<string>>();
        // private BarForm frm = null!;
        // private BarModel[] bars = Array.Empty<BarModel>();
        // public BarModel? Bar { get; set; }
        // private IStorage Storage => GetRequiredService<IStorage>();
        //
        // public BarRepository ScopedBarRepository => GetRequiredService<BarRepository>();
        //
        //
        //
        // protected override async Task InitializeAsync()
        // {
        //     await base.InitializeAsync();
        //     var result = await GetRequiredService<BarRepository>().GetAllAsync();
        //     if (result.itemsCount == 0)
        //     {
        //         await GetRequiredService<BarRepository>().AddAsync(new BarModel { Bar = "Bar", Id = Guid.NewGuid() });
        //         result = await GetRequiredService<BarRepository>().GetAllAsync();
        //     }
        //
        //     Bars = result.items;
        //     barFilter = (await GetRequiredService<BarContext>().Bars.Select(a => a.Bar).Distinct().ToListAsync())
        //         .Select(x => new TableFilter<string> { Text = x, Value = x }).ToArray();
        //     Bar = Bars.OrderBy(b => b.Id).First();
        // }
        //
        // public BarModel[] Bars
        // {
        //     get => bars;
        //     set => bars = value;
        // }
        //
        // private static Task<object> GenerateMetadataAsync()
        // {
        //     var metadata = new BarStorageMetadata();
        //     return Task.FromResult<object>(metadata);
        // }
        //
        //
    }

    public class BarStorageMetadata
    {
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
