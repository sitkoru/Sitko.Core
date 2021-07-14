using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AntDesign;
using AntDesign.TableModels;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Sitko.Core.App.Blazor.Components;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public abstract class BaseAntListComponent<TItem> : BaseComponent where TItem : class
    {
        protected IEnumerable<TItem> Items { get; private set; } = Array.Empty<TItem>();
        public int Count { get; protected set; }

        protected Table<TItem>? Table { get; set; }

        private QueryModel? lastQueryModel;

        private MethodInfo? sortMethod;
        private Task<(TItem[] items, int itemsCount)>? loadTask;
        private bool isTableInitialized;

        [Parameter] public int PageSize { get; set; } = 50;
        [Parameter] public int PageIndex { get; set; } = 1;

        protected override void Initialize()
        {
            base.Initialize();
            var method = typeof(ITableSortModel).GetMethod("SortList", BindingFlags.NonPublic | BindingFlags.Instance);
            if (method is null)
            {
                throw new Exception("Method SortList not found");
            }

            sortMethod = method.MakeGenericMethod(typeof(TItem));
        }

        public Task InitializeTableAsync(QueryModel queryModel)
        {
            if (!isTableInitialized)
            {
                isTableInitialized = true;
                return OnChangeAsync(queryModel);
            }

            return Task.CompletedTask;
        }

        protected async Task OnChangeAsync(QueryModel? queryModel)
        {
            if (!isTableInitialized)
            {
                return;
            }

            await StartLoadingAsync();
            List<Func<IQueryable<TItem>, IQueryable<TItem>>> filters = new();
            List<Func<IQueryable<TItem>, IOrderedQueryable<TItem>>> sorts = new();

            if (queryModel is not null)
            {
                if (sortMethod is not null)
                {
                    foreach (var sortEntry in queryModel.SortModel.Where(s =>
                        s.Sort is not null))
                    {
                        sorts.Add(items =>
                        {
                            var sortResult = sortMethod.Invoke(sortEntry, new object?[] {items});
                            if (sortResult is IOrderedQueryable<TItem> orderedQueryable)
                            {
                                return orderedQueryable;
                            }

                            throw new Exception("Error sorting model");
                        });
                    }
                }

                filters.AddRange(queryModel.FilterModel.Select(filterEntry =>
                    (Func<IQueryable<TItem>, IQueryable<TItem>>)filterEntry.FilterList));
            }

            var page = queryModel?.PageIndex ?? PageIndex;
            var request = new LoadRequest<TItem>(page, filters, sorts);
            lastQueryModel = queryModel;
            try
            {
                loadTask = GetDataAsync(request);
                var (items, itemsCount) = await loadTask;
                Items = items;
                Count = itemsCount;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error loading list data: {ErrorText}", e.ToString());
            }

            await StopLoadingAsync();
        }

        public async Task RefreshAsync(int? page = null)
        {
            if (page is not null)
            {
                PageIndex = page.Value;
            }

            await OnChangeAsync(lastQueryModel);
        }

        protected abstract Task<(TItem[] items, int itemsCount)> GetDataAsync(LoadRequest<TItem> request,
            CancellationToken cancellationToken = default);

        protected override async Task DisposeAsync(bool disposing)
        {
            await base.DisposeAsync(disposing);
            if (loadTask is not null)
            {
                await loadTask;
            }
        }
    }

    public class LoadRequest<TItem> where TItem : class
    {
        public LoadRequest(int page, List<Func<IQueryable<TItem>, IQueryable<TItem>>> filters,
            List<Func<IQueryable<TItem>, IOrderedQueryable<TItem>>> sort)
        {
            Page = page;
            Filters = filters;
            Sort = sort;
        }

        public int Page { get; }
        public List<Func<IQueryable<TItem>, IQueryable<TItem>>> Filters { get; }
        public List<Func<IQueryable<TItem>, IOrderedQueryable<TItem>>> Sort { get; }
    }
}
