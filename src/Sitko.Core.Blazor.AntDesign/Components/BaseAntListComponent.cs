using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AntDesign;
using AntDesign.TableModels;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
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

        [Parameter] public int PageSize { get; set; } = 50;
        [Parameter] public int PageIndex { get; set; } = 1;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            _loadingTask = LoadDataAsync();
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
                                await GetDataAsync(item.OrderBy, item.Page, _cts.Token);
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
            var orderBy = queryModel is not null
                ? string.Join(",",
                    queryModel.SortModel
                        .Where(s => s.Sort is not null)
                        .OrderBy(s => s.Priority)
                        .Select(s =>
                            $"{(s.Sort == SortDirection.Descending.Name ? "-" : "")}{s.FieldName.ToLowerInvariant()}"))
                : "";
            if (string.IsNullOrEmpty(orderBy))
            {
                orderBy = "";
            }

            var page = queryModel?.PageIndex ?? PageIndex;
            Logger.LogDebug("Load page {Page} with order {OrderBy}", page, orderBy);
            var result = _loadChannel.Writer.TryWrite(new LoadRequest(orderBy, page));
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

        protected abstract Task<(TItem[] items, int itemsCount)> GetDataAsync(string orderBy, int page = 1,
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

    internal class LoadRequest
    {
        public LoadRequest(string orderBy, int page)
        {
            OrderBy = orderBy;
            Page = page;
        }

        public string OrderBy { get; }
        public int Page { get; }
    }
}
