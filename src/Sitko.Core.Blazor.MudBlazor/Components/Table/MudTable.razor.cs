using System.Linq.Expressions;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;
using Sitko.Core.Repository;

// ReSharper disable once CheckNamespace
namespace Sitko.Core.Blazor.MudBlazorComponents;

public abstract partial class MudTable<TItem, TFilter> where TFilter : MudTableFilter, new()
{
    private const string SortParam = "sort";
    private const string PageParam = "page";
    private const string PageSizeParam = "pageSize";
    private const string SearchParam = "query";

    private int perPage = 50;
    private int currentPage;
    protected MudTable<TItem?> Table { get; set; } = null!;

    [EditorRequired] [Parameter] public RenderFragment<TItem?>? ChildContent { get; set; }

    [Parameter] public Func<TableState, TFilter, Task>? OnDataLoaded { get; set; }

    [Parameter] public string? Title { get; set; }
    [Parameter] public bool EnableSearch { get; set; } = true;

    [EditorRequired] [Parameter] public RenderFragment? HeaderContent { get; set; }

    [Parameter] public RenderFragment<TableGroupData<object, TItem>?>? GroupHeaderTemplate { get; set; }

    [Parameter] public RenderFragment<TableGroupData<object, TItem>?>? GroupFooterTemplate { get; set; }

    [Parameter] public RenderFragment? FooterContent { get; set; }

    [Parameter] public RenderFragment? NoRecordsContent { get; set; }

    [Parameter] public RenderFragment? LoadingContent { get; set; }

    [Parameter] public RenderFragment? PagerContent { get; set; }

    [Parameter] public RenderFragment? ToolBarContent { get; set; }
    [Parameter] public bool Dense { get; set; }
    [Parameter] public bool Hover { get; set; }
    [Parameter] public bool Bordered { get; set; }
    [Parameter] public bool Striped { get; set; }
    [Parameter] public bool FixedHeader { get; set; }
    [Parameter] public bool FixedFooter { get; set; }
    [Parameter] public bool Virtualize { get; set; }
    [Parameter] public EventCallback<HashSet<TItem>> SelectedItemsChanged { get; set; }
    [Parameter] public Action<TFilter>? FilterChanged { get; set; }
    [Parameter] public bool MultiSelection { get; set; }
    [Parameter] public TFilter Filter { get; set; } = new();
    [Parameter] public Color LoadingProgressColor { get; set; } = Color.Primary;

    private bool HasToolbar => !string.IsNullOrEmpty(Title) || ToolBarContent != null || EnableSearch;

    protected TableState LastState { get; private set; } = new();
    protected TFilter LastFilter { get; private set; } = new();
    [Parameter] public string Class { get; set; } = "";
    [Parameter] public Breakpoint Breakpoint { get; set; } = Breakpoint.Xs;
    [Parameter] public int Elevation { get; set; }
    [Parameter] public string Height { get; set; } = "";
    [Parameter] public bool Outlined { get; set; }

    [Parameter] public bool Square { get; set; }
    [Parameter] public string Style { get; set; } = "";
    [Parameter] public bool AllowUnsorted { get; set; }

    [Parameter] public object Tag { get; set; } = new { };

    [Parameter]
#pragma warning disable BL0007
    public int RowsPerPage
#pragma warning restore BL0007
    {
        get => perPage;
        set
        {
            if (perPage == value)
            {
                return;
            }

            perPage = value;
            RowsPerPageChanged.InvokeAsync(value);
        }
    }

    [Parameter] public EventCallback<int> RowsPerPageChanged { get; set; }

    [Parameter]
#pragma warning disable BL0007
    public int CurrentPage
#pragma warning restore BL0007
    {
        get => currentPage;
        set
        {
            if (currentPage == value)
            {
                return;
            }

            currentPage = value;
            CurrentPageChanged.InvokeAsync(value);
        }
    }
    [Parameter] public EventCallback<int> CurrentPageChanged { get; set; }


    // [Parameter] public int CurrentPage { get; set; } = 1; TODO: until https://github.com/MudBlazor/MudBlazor/issues/1403
    [Parameter] public bool CustomFooter { get; set; }
    [Parameter] public bool CustomHeader { get; set; }
    [Parameter] public string FooterClass { get; set; } = "";
    [Parameter] public TableGroupDefinition<TItem?>? GroupBy { get; set; }

    [Parameter] public string HeaderClass { get; set; } = "";
    [Parameter] public bool HorizontalScrollbar { get; set; }
    [Parameter] public bool ReadOnly { get; set; }
    [Parameter] public string RowClass { get; set; } = "";
    [Parameter] public string RowStyle { get; set; } = "";
    [Parameter] public string SortLabel { get; set; } = "";
    [Parameter] public Dictionary<string, object>? UserAttributes { get; set; }
    [Parameter] public string GroupFooterClass { get; set; } = "";
    [Parameter] public string GroupHeaderClass { get; set; } = "";
    [Parameter] public string GroupFooterStyle { get; set; } = "";
    [Parameter] public string GroupHeaderStyle { get; set; } = "";
    [Parameter] public Func<TItem?, int, string>? RowStyleFunc { get; set; }
    [Parameter] public Func<TItem?, int, string>? RowClassFunc { get; set; }
    [Parameter] public EventCallback<TableRowClickEventArgs<TItem?>> OnRowClick { get; set; }
    [Parameter] public HashSet<TItem?>? SelectedItems { get; set; }
    [Parameter] public TItem? SelectedItem { get; set; }
    [Parameter] public EventCallback<TItem?> SelectedItemChanged { get; set; }

    [Parameter] public Func<Task<Dictionary<string, object?>>>? AddParamsToUrl { get; set; }
    [Parameter] public Func<Task>? GetParamsFromUrl { get; set; }

    [Parameter] public bool EnableUrlNavigation { get; set; }

    protected bool IsFirstLoad { get; set; } = true;

    private int RowsPerPageFinal
    {
        get
        {
            if (IsFirstLoad && EnableUrlNavigation && TryGetQueryString<int?>(PageSizeParam, out var pageSize) && pageSize > 0)
            {
                return pageSize.Value;
            }

            return RowsPerPage;
        }
    }

    private int CurrentPageFinal
    {
        get
        {
            if (IsFirstLoad && EnableUrlNavigation && TryGetQueryString<int?>(PageParam, out var page) && page > 0)
            {
                return page.Value - 1;
            }

            return CurrentPage;
        }
    }

    protected async Task DoGetParamsFromUrlAsync(TableState state, CancellationToken cancellationToken = default)
    {
        if (GetParamsFromUrl is not null)
        {
            await GetParamsFromUrl();
        }

        if (TryGetQueryString<string?>(SortParam, out var defaultSort) && !string.IsNullOrEmpty(defaultSort))
        {
            if (defaultSort.StartsWith("-", StringComparison.InvariantCulture))
            {
                state.SortDirection = SortDirection.Descending;
                state.SortLabel = defaultSort.Remove(0, 1);
            }
            else
            {
                state.SortDirection = SortDirection.Ascending;
                state.SortLabel = defaultSort;
            }
        }

        if (TryGetQueryString<int?>(PageSizeParam, out var defaultPageSize))
        {
            state.PageSize = defaultPageSize.Value;
            RowsPerPage = state.PageSize;
        }

        if (TryGetQueryString<int?>(PageParam, out var defaultPage))
        {
            state.Page = defaultPage.Value - 1;
            CurrentPage = state.Page;
        }

        if (TryGetQueryString<string?>(SearchParam, out var defaultQuery) && !string.IsNullOrEmpty(defaultQuery))
        {
            Filter.Search = defaultQuery;
        }
    }

    protected async Task DoAddUrlParamsAsync(TableState state, CancellationToken cancellationToken = default)
    {
        if (!IsFirstLoad)
        {
            var urlParams = new Dictionary<string, object?>();
            if (AddParamsToUrl is not null)
            {
                urlParams = await AddParamsToUrl();
            }

            switch (state.SortDirection)
            {
                case SortDirection.Ascending:
                    urlParams.Add(SortParam, state.SortLabel);
                    break;
                case SortDirection.Descending:
                    urlParams.Add(SortParam, $"-{state.SortLabel}");
                    break;
            }

            urlParams.Add(PageParam, state.Page + 1);
            urlParams.Add(PageSizeParam, state.PageSize);
            urlParams.Add(SearchParam, Filter.Search);

            var url = NavigationManager.GetUriWithQueryParameters(urlParams);

            NavigationManager.NavigateTo(url, replace: true);
        }
    }

    private async Task<TableData<TItem?>> ServerReloadAsync(TableState state,
        CancellationToken cancellationToken = default)
    {
        if (IsFirstLoad && EnableUrlNavigation)
        {
            await DoGetParamsFromUrlAsync(state, cancellationToken);
        }
        else
        {
            RowsPerPage = state.PageSize;
            CurrentPage = state.Page;
        }

        await StartLoadingAsync(cancellationToken);
        var result = await GetDataAsync(state, Filter, cancellationToken);
        await StopLoadingAsync(cancellationToken);
        LastState = state;
        LastFilter = Filter;
        if (OnDataLoaded is not null)
        {
            Logger.LogDebug("Execute OnDataLoaded");
            await OnDataLoaded(LastState, LastFilter);
        }

        IsFirstLoad = false;

        return new TableData<TItem?> { Items = result.items, TotalItems = result.itemsCount };
    }


    protected abstract Task<(TItem[] items, int itemsCount)> GetDataAsync(TableState state, TFilter filter,
        CancellationToken cancellationToken = default);

    private Task OnSearchAsync(string text) => UpdateFilterAsync(filter => filter.Search = text);

    public Task RefreshAsync() => Table.ReloadServerData();

    public async Task UpdateFilterAsync(Action<TFilter> updateFilter)
    {
        updateFilter(Filter);
        FilterChanged?.Invoke(Filter);

        await Table.ReloadServerData();
    }
}

public class
    MudRepositoryTable<TEntity, TEntityPk, TRepository> : MudRepositoryTable<TEntity, TEntityPk, TRepository
    , MudTableFilter>
    where TEntity : class, IEntity<TEntityPk>
    where TRepository : IRepository<TEntity, TEntityPk>
    where TEntityPk : notnull;

public abstract class MudRepositoryTable<TEntity, TEntityPk, TRepository, TFilter> : MudTable<TEntity, TFilter>
    where TEntity : class, IEntity<TEntityPk>
    where TRepository : IRepository<TEntity, TEntityPk>
    where TFilter : MudTableFilter, new()
    where TEntityPk : notnull
{
    [Parameter] public Func<IRepositoryQuery<TEntity>, Task>? ConfigureQuery { get; set; }

    protected Task<TResult> ExecuteRepositoryOperationAsync<TResult>(
        Func<TRepository, Task<TResult>> operation) =>
        ExecuteServiceOperation(operation);

    protected override Task<(TEntity[] items, int itemsCount)> GetDataAsync(TableState state,
        TFilter filter, CancellationToken cancellationToken = default) =>
        ExecuteRepositoryOperationAsync(repository =>
        {
            return repository.GetAllAsync(async query =>
            {
                await DoConfigureQueryAsync(state, filter, query, cancellationToken);


                query.Paginate(state.Page + 1, state.PageSize);
            }, cancellationToken);
        });

    private async Task DoConfigureQueryAsync(TableState state, TFilter filter,
        IRepositoryQuery<TEntity> query, CancellationToken cancellationToken = default)
    {
        if (ConfigureQuery is not null)
        {
            await ConfigureQuery(query);
        }

        await ConfigureQueryAsync(query, filter, cancellationToken);

        if (state.SortDirection == SortDirection.Ascending)
        {
            query.OrderByString(state.SortLabel);
        }
        else if (state.SortDirection == SortDirection.Descending)
        {
            query.OrderByString($"-{state.SortLabel}");
        }

        if (!IsFirstLoad && EnableUrlNavigation)
        {
            await DoAddUrlParamsAsync(state, cancellationToken);
        }
    }

    protected virtual Task ConfigureQueryAsync(IRepositoryQuery<TEntity> query, TFilter filter,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    protected Task<TResult> ExecuteRepositoryOperation<TResult>(
        Func<TRepository, Task<TResult>> operation) =>
        ExecuteServiceOperation(operation);

    public Task<int> SumAsync(Expression<Func<TEntity, int>> selector) =>
        ExecuteRepositoryOperation(repository =>
        {
            return repository.SumAsync(async query =>
            {
                await DoConfigureQueryAsync(LastState, LastFilter, query);
            }, selector);
        });

    public Task<int?> SumAsync(Expression<Func<TEntity, int?>> selector) =>
        ExecuteRepositoryOperation(repository =>
        {
            return repository.SumAsync(async query =>
            {
                await DoConfigureQueryAsync(LastState, LastFilter, query);
            }, selector);
        });

    public Task<long> SumAsync(Expression<Func<TEntity, long>> selector) =>
        ExecuteRepositoryOperation(repository =>
        {
            return repository.SumAsync(async query =>
            {
                await DoConfigureQueryAsync(LastState, LastFilter, query);
            }, selector);
        });

    public Task<long?> SumAsync(Expression<Func<TEntity, long?>> selector) =>
        ExecuteRepositoryOperation(repository =>
        {
            return repository.SumAsync(async query =>
            {
                await DoConfigureQueryAsync(LastState, LastFilter, query);
            }, selector);
        });

    public Task<double> SumAsync(Expression<Func<TEntity, double>> selector) =>
        ExecuteRepositoryOperation(repository =>
        {
            return repository.SumAsync(async query =>
            {
                await DoConfigureQueryAsync(LastState, LastFilter, query);
            }, selector);
        });

    public Task<double?> SumAsync(Expression<Func<TEntity, double?>> selector) =>
        ExecuteRepositoryOperation(repository =>
        {
            return repository.SumAsync(async query =>
            {
                await DoConfigureQueryAsync(LastState, LastFilter, query);
            }, selector);
        });

    public Task<float> SumAsync(Expression<Func<TEntity, float>> selector) =>
        ExecuteRepositoryOperation(repository =>
        {
            return repository.SumAsync(async query =>
            {
                await DoConfigureQueryAsync(LastState, LastFilter, query);
            }, selector);
        });

    public Task<float?> SumAsync(Expression<Func<TEntity, float?>> selector) =>
        ExecuteRepositoryOperation(repository =>
        {
            return repository.SumAsync(async query =>
            {
                await DoConfigureQueryAsync(LastState, LastFilter, query);
            }, selector);
        });

    public Task<decimal> SumAsync(Expression<Func<TEntity, decimal>> selector) =>
        ExecuteRepositoryOperation(repository =>
        {
            return repository.SumAsync(async query =>
            {
                await DoConfigureQueryAsync(LastState, LastFilter, query);
            }, selector);
        });

    public Task<decimal?> SumAsync(Expression<Func<TEntity, decimal?>> selector) =>
        ExecuteRepositoryOperation(repository =>
        {
            return repository.SumAsync(async query =>
            {
                await DoConfigureQueryAsync(LastState, LastFilter, query);
            }, selector);
        });
}

public record MudTableFilter
{
    public string? Search { get; set; }
}

[PublicAPI]
public static class ListFilterExtensions
{
    public static void Toggle<T>(this List<T> list, T value)
    {
        if (list.Contains(value))
        {
            list.Remove(value);
        }
        else
        {
            list.Add(value);
        }
    }
}
