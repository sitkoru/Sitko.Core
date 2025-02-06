using System.Globalization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Sitko.Core.Apps.Blazor.Client.Components.Lists;
using Sitko.Core.Apps.Blazor.Data.Entities;
using Sitko.Core.Blazor.MudBlazorComponents;
using Sitko.Core.Repository;

namespace Sitko.Core.Apps.Blazor.Client.Pages
{
    public partial class Index
    {
        private int rowsPerPage = 10;
        private const string FilterParamId = "id";
        private const string FilterParamTitle = "title";
        private const string FilterParamDateRange = "dateRange";

        [Parameter]
        [SupplyParameterFromQuery(Name = FilterParamId)]
        public Guid? Id { get; set; }

        [Parameter]
        [SupplyParameterFromQuery(Name = FilterParamTitle)]
        public string? Title { get; set; }

        [Parameter]
        [SupplyParameterFromQuery(Name = FilterParamDateRange)]
        public string? DateRange { get; set; }

        private BarRepositoryList barList = null!;
        private decimal Summary { get; set; }

        private async Task CountSummaryAsync(TableState state, MudTableFilter filter) =>
            Summary = await barList.SumAsync(model => model.Sum);

        private FilterList FilterList { get; set; } = new();
        private MudAutocomplete<BarModel> IdFilterAutocomplete { get; set; } = null!;

        [Inject] private IRepository<BarModel, Guid> BarRepository { get; set; } = null!;
        private (BarModel[] items, int itemsCount) bars;

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            bars = await BarRepository.GetAllAsync();
        }

        private async Task<IEnumerable<BarModel>> SearchIdsAsync(string? value, CancellationToken cancellationToken) => string.IsNullOrEmpty(value)
            ? (await BarRepository.GetAllAsync(cancellationToken)).items
            : (await BarRepository.GetAllAsync(q => q.Where(b => b.Id == Guid.Parse(value)), cancellationToken)).items;

        private async Task ChangeDateAsync(DateRange? dateRange)
        {
            FilterList.DateRange = dateRange;
            await barList.RefreshAsync();
        }

        private async Task ChangeIdAsync(Guid? id)
        {
            FilterList.Model = bars.items.FirstOrDefault(b => id != null && b.Id == id);
            await IdFilterAutocomplete.ToggleMenuAsync();
            await barList.RefreshAsync();
        }

        private async Task SearchTitleAsync(string value)
        {
            FilterList.Title = value;
            await barList.RefreshAsync();
        }

        private Task ConfigureQueryAsync(IRepositoryQuery<BarModel> query)
        {
            query.Where(model => model.Bar != "");
            if (FilterList.Model is not null && FilterList.Model.Id != Guid.Empty)
            {
                query.Where(p => p.Id == FilterList.Model.Id);
            }

            if (!string.IsNullOrEmpty(FilterList.Title))
            {
                query.Where(p => p.Bar == FilterList.Title);
            }

            if (FilterList.DateRange is not null)
            {
                query.Where(p => p.Date >= new DateTimeOffset((DateTime)FilterList.DateRange.Start!).UtcDateTime &&
                                 p.Date <= new DateTimeOffset((DateTime)FilterList.DateRange.End!).UtcDateTime);
            }

            return Task.CompletedTask;
        }

        private Task<Dictionary<string, object?>> AddParamsToUrlAsync()
        {
            var urlParams = new Dictionary<string, object?>();
            urlParams.Add(FilterParamId, FilterList.Model?.Id);
            urlParams.Add(FilterParamTitle, FilterList.Title);
            if (FilterList.DateRange != null)
            {
                urlParams.Add(FilterParamDateRange,
                    $"{FilterList.DateRange.Start.ToString()}-{FilterList.DateRange.End.ToString()}");
            }
            else
            {
                urlParams.Add(FilterParamDateRange, null);
            }

            return Task.FromResult(urlParams);
        }

        private Task GetParamsFromUrlAsync()
        {
            var hasChanged = false;
            if (Id != null)
            {
                FilterList.Model = bars.items.FirstOrDefault(b => b.Id == Id);
                hasChanged = true;
            }

            if (!string.IsNullOrEmpty(Title))
            {
                FilterList.Title = Title;
                hasChanged = true;
            }

            if (!string.IsNullOrEmpty(DateRange))
            {
                var dateData = DateRange.Split("-");
                if (dateData.Length == 2)
                {
                    FilterList.DateRange = new DateRange(DateTime.Parse(dateData[0], CultureInfo.InvariantCulture),
                        DateTime.Parse(dateData[1], CultureInfo.InvariantCulture));
                    hasChanged = true;
                }
            }

            if (hasChanged)
            {
                StateHasChanged();
            }

            return Task.CompletedTask;
        }
    }

    public class BarStorageMetadata
    {
        public Guid Id { get; set; } = Guid.NewGuid();
    }

    public class FilterList
    {
        public BarModel? Model { get; set; }
        public string? Title { get; set; }
        public DateRange? DateRange { get; set; }
    }
}
