using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AntDesign;
using AntDesign.TableModels;
using Microsoft.AspNetCore.Components;
using Sitko.Core.App.Blazor.Components;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public abstract class BaseAntListComponent<TItem> : BaseComponent, IAsyncDisposable where TItem : class
    {
        protected IEnumerable<TItem> Items = new TItem[0];
        public int Count { get; protected set; }

        protected Table<TItem> Table { get; set; }

        private readonly Channel<LoadRequest<TItem>>
            _loadChannel = Channel.CreateUnbounded<LoadRequest<TItem>>();

        private readonly CancellationTokenSource _cts = new();
        private QueryModel<TItem>? _lastQueryModel;
        private Task? _loadingTask;
        private MethodInfo? _sortMethod;

        [Parameter] public int PageSize { get; set; } = 50;
        [Parameter] public int PageIndex { get; set; } = 1;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            _loadingTask = LoadDataAsync();
            var method = typeof(ITableSortModel).GetMethod("SortList", BindingFlags.NonPublic | BindingFlags.Instance);
            if (method is null)
            {
                throw new Exception("Method SortList not found");
            }

            _sortMethod = method.MakeGenericMethod(typeof(TItem));
            MarkAsInitialized();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                while (await _loadChannel.Reader.WaitToReadAsync(_cts.Token).ConfigureAwait(false))
                {
                    while (_loadChannel.Reader.TryRead(out var item))
                    {
                        try
                        {
                            await StartLoadingAsync();
                            (var items, int itemsCount) =
                                await GetDataAsync(item, _cts.Token);
                            Items = items;
                            Count = itemsCount;
                            await StopLoadingAsync();
                        }
                        catch (OperationCanceledException)
                        {
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        protected void OnChange(QueryModel<TItem>? queryModel)
        {
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
            var result = _loadChannel.Writer.TryWrite(new LoadRequest<TItem>(page, filters, sorts));
            if (!result)
            {
                throw new Exception("Bla");
            }

            _lastQueryModel = queryModel;
        }

        public void Refresh(int? page = null)
        {
            if (page is not null)
            {
                PageIndex = page.Value;
            }

            OnChange(_lastQueryModel);
        }

        protected abstract Task<(TItem[] items, int itemsCount)> GetDataAsync(LoadRequest<TItem> request,
            CancellationToken cancellationToken = default);

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            if (_loadingTask is not null)
            {
                await _loadingTask;
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
