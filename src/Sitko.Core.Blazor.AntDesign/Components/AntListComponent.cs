using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AntDesign;
using AntDesign.TableModels;
using Sitko.Core.App.Blazor.Components;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public abstract class AntListComponent<TItem> : BaseComponent where TItem : class
    {
        protected IEnumerable<TItem> Items;
        protected int Count;
        protected Pagination _pagination;
        protected int PageSize { get; set; } = 50;
        protected string DefaultSorting { get; set; } = "";

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await LoadDataAsync();
            MarkAsInitialized();
        }

        protected async Task OnChange(QueryModel<TItem> queryModel)
        {
            var orderBy = string.Join(",",
                queryModel.SortModel
                    .Where(s => s.Sort is not null)
                    .OrderBy(s => s.Priority)
                    .Select(s =>
                        $"{(s.Sort == SortDirection.Descending.Name ? "-" : "")}{s.FieldName.ToLowerInvariant()}"));
            await LoadDataAsync(string.IsNullOrEmpty(orderBy) ? DefaultSorting : orderBy,
                queryModel.PageIndex);
        }

        protected async Task LoadDataAsync(string orderBy = "", int page = 1)
        {
            StartLoading();
            (var items, int itemsCount) = await GetDataAsync(orderBy, page);
            Items = items;
            Count = itemsCount;
            StopLoading();
        }

        protected abstract Task<(TItem[] items, int itemsCount)> GetDataAsync(string orderBy, int page = 1);
    }
}
