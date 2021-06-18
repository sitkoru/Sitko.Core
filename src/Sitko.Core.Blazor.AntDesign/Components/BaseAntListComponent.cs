using System;
using System.Collections.Generic;
using System.Linq;
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

        private readonly Channel<(string orderBy, int page)>
            _loadChannel = Channel.CreateUnbounded<(string orderBy, int page)>();

        private readonly CancellationTokenSource _cts = new();
        private Task? _loadTask;
        private QueryModel<TItem>? _lastQueryModel;

        [Parameter] public int PageSize { get; set; } = 50;
        [Parameter] public int PageIndex { get; set; } = 1;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            _loadTask = LoadDataAsync();
            MarkAsInitialized();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                while (await _loadChannel.Reader.WaitToReadAsync(_cts.Token).ConfigureAwait(false))
                {
                    while (_loadChannel.Reader.TryRead(out (string orderBy, int page) item))
                    {
                        try
                        {
                            await StartLoadingAsync();
                            (var items, int itemsCount) =
                                await GetDataAsync(item.orderBy, item.page, _cts.Token);
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
            _loadChannel.Writer.TryWrite((string.IsNullOrEmpty(orderBy) ? "" : orderBy,
                queryModel?.PageIndex ?? PageIndex));
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
            if (_loadTask is not null)
            {
                await _loadTask;
            }
        }
    }
}
