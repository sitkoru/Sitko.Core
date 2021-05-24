using System.Collections.Generic;
using System.Threading.Tasks;
using AntDesign;
using Sitko.Core.App.Blazor.Components;

namespace Sitko.Core.Blazor.AntDesign.Components
{
    public abstract class AntListPage<TItem> : BaseComponent where TItem : class
    {
        protected IEnumerable<TItem> Items;
        protected int Count;
        protected Pagination _pagination;
        protected int PageSize { get; set; } = 50;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await LoadDataAsync();
            MarkAsInitialized();
        }

        protected async Task LoadDataAsync(int page = 1)
        {
            StartLoading();
            (var items, int itemsCount) = await GetDataAsync();
            Items = items;
            Count = itemsCount;
            StopLoading();
        }

        protected abstract Task<(IEnumerable<TItem> items, int totalItemsCount)> GetDataAsync();
    }
}
