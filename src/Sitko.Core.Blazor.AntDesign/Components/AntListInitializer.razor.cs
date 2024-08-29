using AntDesign;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Sitko.Core.Blazor.Components;

namespace Sitko.Core.Blazor.AntDesignComponents.Components;

public class AntListInitializer<TEntity> : BaseComponent where TEntity : class
{
    [EditorRequired]
    [Parameter]
    public BaseAntListComponent<TEntity> AntListComponent { get; set; } = null!;
    [EditorRequired]
    [Parameter]
    public Table<TEntity> Table { get; set; } = null!;

    protected override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        Logger.LogDebug("Try to initialize table {Table}", Table.GetType());
        var columns = Table.ColumnContext.HeaderColumns.OfType<IFieldColumn>().ToArray();
        if (columns.Any())
        {
            Logger.LogDebug("Table {Table} has columns", Table.GetType());
            var sortable = columns.Count(c => c.Sortable);
            if (sortable > 0)
            {
                Logger.LogDebug("Table {Table} has sortable columns", Table.GetType());
                var withSortModel = columns.Count(c => c.SortModel is not null);
                if (sortable == withSortModel)
                {
                    Logger.LogDebug("Table {Table} columns has sort model values set", Table.GetType());
                    var queryModel = Table.GetQueryModel();
                    if (queryModel.SortModel.Count > 0)
                    {
                        Logger.LogDebug("Table {Table} query model has sort model filled", Table.GetType());
                        await AntListComponent.InitializeTableAsync(Table.GetQueryModel());
                        Logger.LogDebug("Table {Table} initialized", Table.GetType());
                    }
                }
            }
            else
            {
                Logger.LogDebug("Table {Table} has no sortable columns", Table.GetType());
                await AntListComponent.InitializeTableAsync(Table.GetQueryModel());
                Logger.LogDebug("Table {Table} initialized", Table.GetType());
            }
        }
    }
}
