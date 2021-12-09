using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;
using Sitko.Core.Repository;

namespace Sitko.Core.Blazor.MudBlazorComponents;

public abstract partial class MudTable<TItem, TFilter> where TFilter : MudTableFilter, new()
{
    protected MudTable<TItem> Table { get; set; } = null!;

    [EditorRequired] [Parameter] public RenderFragment<TItem>? ChildContent { get; set; }

    [Parameter] public Func<Task>? OnDataLoaded { get; set; }

    [Parameter] public string? Title { get; set; }
    [Parameter] public bool EnableSearch { get; set; } = true;
#if NET6_0_OR_GREATER
    [EditorRequired]
#endif
    [Parameter]
    public RenderFragment? HeaderContent { get; set; }

    [Parameter] public RenderFragment? FooterContent { get; set; }

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
    [Parameter] public int RowsPerPage { get; set; } = 50;
    // [Parameter] public int CurrentPage { get; set; } = 1; TODO: until https://github.com/MudBlazor/MudBlazor/issues/1403
    [Parameter] public bool CustomFooter { get; set; }
    [Parameter] public bool CustomHeader { get; set; }
    [Parameter] public string FooterClass { get; set; } = "";
    [Parameter] public TableGroupDefinition<TItem>? GroupBy { get; set; }

    [Parameter] public string HeaderClass { get; set; } = "";
    [Parameter] public string QuickColumns { get; set; } = "";
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
    [Parameter] public Func<TItem, int, string>? RowStyleFunc { get; set; }
    [Parameter] public Func<TItem, int, string>? RowClassFunc { get; set; }
    [Parameter] public EventCallback<TableRowClickEventArgs<TItem>> OnRowClick { get; set; }
    [Parameter] public HashSet<TItem>? SelectedItems { get; set; }
    [Parameter] public TItem? SelectedItem { get; set; }
    [Parameter] public EventCallback<TItem> SelectedItemChanged { get; set; }

    private async Task<TableData<TItem>> ServerReloadAsync(TableState state)
    {
        await StartLoadingAsync();
        var result = await GetDataAsync(state, Filter);
        await StopLoadingAsync();
        LastState = state;
        LastFilter = Filter;
        if (OnDataLoaded is not null)
        {
            Logger.LogDebug("Execute OnDataLoaded");
            await OnDataLoaded();
        }

        return new TableData<TItem> { Items = result.items, TotalItems = result.itemsCount };
    }


    protected abstract Task<(TItem[] items, int itemsCount)> GetDataAsync(TableState state, TFilter filter);

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
    where TEntity : class, IEntity<TEntityPk>, new()
    where TRepository : IRepository<TEntity, TEntityPk>
{
}

public abstract class MudRepositoryTable<TEntity, TEntityPk, TRepository, TFilter> : MudTable<TEntity, TFilter>
    where TEntity : class, IEntity<TEntityPk>, new()
    where TRepository : IRepository<TEntity, TEntityPk>
    where TFilter : MudTableFilter, new()
{
    [Parameter] public Func<IRepositoryQuery<TEntity>, Task>? ConfigureQuery { get; set; }

    protected Task<TResult> ExecuteRepositoryOperationAsync<TResult>(
        Func<TRepository, Task<TResult>> operation) =>
        ExecuteServiceOperation(operation);

    protected override Task<(TEntity[] items, int itemsCount)> GetDataAsync(TableState state,
        TFilter filter) =>
        ExecuteRepositoryOperationAsync(repository =>
        {
            return repository.GetAllAsync(async query =>
            {
                await DoConfigureQueryAsync(state, filter, query);


                query.Paginate(state.Page + 1, state.PageSize);
            });
        });

    private async Task DoConfigureQueryAsync(TableState state, TFilter filter,
        IRepositoryQuery<TEntity> query)
    {
        if (ConfigureQuery is not null)
        {
            await ConfigureQuery(query);
        }

        await ConfigureQueryAsync(query, filter);

        if (state.SortDirection == SortDirection.Ascending)
        {
            query.OrderByString(state.SortLabel);
        }
        else if (state.SortDirection == SortDirection.Descending)
        {
            query.OrderByString($"-{state.SortLabel}");
        }
    }

    protected virtual Task ConfigureQueryAsync(IRepositoryQuery<TEntity> query, TFilter filter) =>
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
