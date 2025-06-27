using System.Reflection;
using AntDesign;
using AntDesign.TableModels;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Sitko.Core.Blazor.Components;

namespace Sitko.Core.Blazor.AntDesignComponents.Components;

public abstract partial class BaseAntListComponent<TItem> where TItem : class
{
    private bool isTableInitialized;

    private QueryModel? lastQueryModel;
    private Task<(TItem[] items, int itemsCount)>? loadTask;

    private MethodInfo? sortMethod;
    protected IEnumerable<TItem> Items { get; private set; } = Array.Empty<TItem>();
    public int Count { get; protected set; }

    protected Table<TItem>? Table { get; set; }

    [Parameter] public int PageSize { get; set; } = 50;
    [Parameter] public int PageIndex { get; set; } = 1;
    [Parameter] public Func<Task>? OnDataLoaded { get; set; }
    [Parameter] public RenderFragment<TItem>? ChildContent { get; set; }

    [Parameter] public RenderFragment<TItem>? RowTemplate { get; set; }

    [Parameter] public RenderFragment<RowData<TItem>>? ExpandTemplate { get; set; }

    [Parameter] public Func<RowData<TItem>, bool> RowExpandable { get; set; } = _ => true;

    [Parameter] public Func<TItem, IEnumerable<TItem>> TreeChildren { get; set; } = _ => Enumerable.Empty<TItem>();

    [Parameter]
    public Func<RowData<TItem>, Dictionary<string, object>> OnRow { get; set; } =
        _ => new Dictionary<string, object>();

    [Parameter]
    public Func<Dictionary<string, object>> OnHeaderRow { get; set; } = () => new Dictionary<string, object>();

    [Parameter] public string? Title { get; set; }

    [Parameter] public RenderFragment? TitleTemplate { get; set; }

    [Parameter] public string? Footer { get; set; }

    [Parameter] public RenderFragment? FooterTemplate { get; set; }

    [Parameter] public TableSize Size { get; set; } = TableSize.Default;

    [Parameter] public TableLocale Locale { get; set; } = LocaleProvider.CurrentLocale.Table;

    [Parameter] public bool Bordered { get; set; }

    [Parameter] public string? ScrollX { get; set; }

    [Parameter] public string? ScrollY { get; set; }

    [Parameter] public string ScrollBarWidth { get; set; } = "17px";

    [Parameter] public int IndentSize { get; set; } = 15;

    [Parameter] public int ExpandIconColumnIndex { get; set; }

    [Parameter] public Func<RowData<TItem>, string> RowClassName { get; set; } = _ => "";

    [Parameter] public Func<RowData<TItem>, string> ExpandedRowClassName { get; set; } = _ => "";

    [Parameter] public EventCallback<RowData<TItem>> OnExpand { get; set; }

    [Parameter] public SortDirection[] SortDirections { get; set; } = [SortDirection.Ascending, SortDirection.Descending, SortDirection.None];

    [Parameter] public string TableLayout { get; set; } = "";

    [Parameter] public EventCallback<RowData<TItem>> OnRowClick { get; set; }

    [Parameter] public bool HidePagination { get; set; }

    [Parameter] public string PaginationPosition { get; set; } = "bottomRight";

    [Parameter] public EventCallback<int> TotalChanged { get; set; }

    [Parameter] public EventCallback<PaginationEventArgs> OnPageIndexChange { get; set; }

    [Parameter] public EventCallback<PaginationEventArgs> OnPageSizeChange { get; set; }

    [Parameter] public IEnumerable<TItem>? SelectedRows { get; set; }

    [Parameter] public EventCallback<IEnumerable<TItem>> SelectedRowsChanged { get; set; }
    public override ScopeType ScopeType { get; set; } = ScopeType.Isolated;

    protected LoadRequest<TItem>? LastRequest { get; set; }

    protected override void Initialize()
    {
        base.Initialize();
        var method = typeof(ITableSortModel).GetMethod("SortList", BindingFlags.NonPublic | BindingFlags.Instance);
        if (method is null)
        {
            throw new MissingMethodException("Method SortList not found");
        }

        sortMethod = method.MakeGenericMethod(typeof(TItem));
        Logger.LogDebug("Sort method stored");
    }

    public Task InitializeTableAsync(QueryModel queryModel)
    {
        Logger.LogDebug("Try to initialize table");
        if (!isTableInitialized)
        {
            Logger.LogDebug("Table is not initialized. Proceed");
            isTableInitialized = true;
            return OnChangeAsync(queryModel);
        }

        Logger.LogDebug("Table already initialized, skip");
        return Task.CompletedTask;
    }

    protected async Task OnChangeAsync(QueryModel? queryModel)
    {
        Logger.LogDebug("Table model changed. Need to load data");
        if (!isTableInitialized)
        {
            Logger.LogDebug("Table is not initialized. Skip");
            return;
        }

        await StartLoadingAsync();
        List<FilterOperation<TItem>> filters = new();
        List<SortOperation<TItem>> sorts = new();
        Logger.LogDebug("Parse query model");
        if (queryModel is not null)
        {
            if (sortMethod is not null)
            {
                foreach (var sortEntry in queryModel.SortModel.Where(s =>
                             s.Sort is not null))
                {
                    sorts.Add(new SortOperation<TItem>(items =>
                    {
                        var sortResult = sortMethod.Invoke(sortEntry, new object?[] { items });
                        if (sortResult is IOrderedQueryable<TItem> orderedQueryable)
                        {
                            return orderedQueryable;
                        }

                        throw new InvalidOperationException("Error sorting model");
                    }));
                }
            }

            foreach (var filterModel in queryModel.FilterModel)
            {
                filters.Add(new FilterOperation<TItem>(items => filterModel.FilterList(items)));
            }
        }

        var page = queryModel?.PageIndex ?? PageIndex;
        var request = new LoadRequest<TItem>(page, filters, sorts);
        Logger.LogDebug("LoadRequest: {@LoadRequest}", request);
        lastQueryModel = queryModel;
        try
        {
            Logger.LogDebug("Run load data task");
            loadTask = GetDataAsync(request);
            var (items, itemsCount) = await loadTask;
            Logger.LogDebug("Data loaded. Count: {Count}", itemsCount);
            Items = items;
            Count = itemsCount;
            LastRequest = request;
            if (OnDataLoaded is not null)
            {
                Logger.LogDebug("Execute OnDataLoaded");
                await OnDataLoaded();
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error loading list data: {ErrorText}", e.ToString());
        }

        Logger.LogDebug("Data load is complete");
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
    public LoadRequest(int page, List<FilterOperation<TItem>> filters,
        List<SortOperation<TItem>> sort)
    {
        Page = page;
        Filters = filters;
        Sort = sort;
    }

    public int Page { get; }
    public List<FilterOperation<TItem>> Filters { get; }
    public List<SortOperation<TItem>> Sort { get; }
}

public class FilterOperation<TItem> where TItem : class
{
    public FilterOperation(Func<IQueryable<TItem>, IQueryable<TItem>> operation) => Operation = operation;

    public Func<IQueryable<TItem>, IQueryable<TItem>> Operation { get; }
}

public class SortOperation<TItem> where TItem : class
{
    public SortOperation(Func<IQueryable<TItem>, IOrderedQueryable<TItem>> operation) => Operation = operation;

    public Func<IQueryable<TItem>, IOrderedQueryable<TItem>> Operation { get; }
}

