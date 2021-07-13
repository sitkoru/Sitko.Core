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

        private QueryModel? _lastQueryModel;

        private MethodInfo? _sortMethod;
        private Task<(TItem[] items, int itemsCount)>? _loadTask;
        private bool _isTableInitialized;

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

            _sortMethod = method.MakeGenericMethod(typeof(TItem));
        }

        public Task InitializeTableAsync(QueryModel queryModel)
        {
            if (!_isTableInitialized)
            {
                _isTableInitialized = true;
                return OnChangeAsync(queryModel);
            }

            return Task.CompletedTask;
        }

        protected async Task OnChangeAsync(QueryModel? queryModel)
        {
            if (!_isTableInitialized) return;
            await StartLoadingAsync();
            List<Func<IQueryable<TItem>, IQueryable<TItem>>> filters = new();
            List<Func<IQueryable<TItem>, IOrderedQueryable<TItem>>> sorts = new();

            if (queryModel is not null)
            {
                if (_sortMethod is not null)
                {
                    foreach (var sortEntry in queryModel.SortModel.Where(s =>
                        s.Sort is not null))
                    {
                        sorts.Add(items =>
                        {
                            var sortResult = _sortMethod.Invoke(sortEntry, new object?[] {items});
                            if (sortResult is IOrderedQueryable<TItem> orderedQueryable)
                            {
                                return orderedQueryable;
                            }

                            throw new Exception("Error sorting model");
                        });
                    }
                }

                filters.AddRange(queryModel.FilterModel.Select(filterEntry =>
                    (Func<IQueryable<TItem>, IQueryable<TItem>>)(filterEntry.FilterList)));
            }

            var page = queryModel?.PageIndex ?? PageIndex;
            var request = new LoadRequest<TItem>(page, filters, sorts);
            _lastQueryModel = queryModel;
            try
            {
                _loadTask = GetDataAsync(request);
                (var items, int itemsCount) = await _loadTask;
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

            await OnChangeAsync(_lastQueryModel);
        }

        protected abstract Task<(TItem[] items, int itemsCount)> GetDataAsync(LoadRequest<TItem> request,
            CancellationToken cancellationToken = default);

        protected override async Task DisposeAsync(bool disposing)
        {
            await base.DisposeAsync(disposing);
            if (_loadTask is not null)
            {
                await _loadTask;
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
