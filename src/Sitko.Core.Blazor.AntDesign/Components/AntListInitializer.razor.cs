using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AntDesign;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Sitko.Core.App.Blazor.Components;
using Tempus;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public class AntListInitializer<TEntity> : BaseComponent where TEntity : class
    {
        [Parameter] public BaseAntListComponent<TEntity> AntListComponent { get; set; } = null!;
        [Parameter] public Table<TEntity> Table { get; set; } = null!;

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            var columns = Table.ColumnContext.HeaderColumns.OfType<IFieldColumn>().ToArray();
            if (columns.Any())
            {
                var sortable = columns.Count(c => c.Sortable);
                if (sortable > 0)
                {
                    var withSortModel = columns.Count(c => c.SortModel is not null);
                    if (sortable == withSortModel)
                    {
                        var queryModel = Table.GetQueryModel();
                        if (queryModel.SortModel.Count > 0)
                        {
                            await AntListComponent.InitializeTableAsync(Table.GetQueryModel());
                        }
                    }
                }
                else
                {
                    await AntListComponent.InitializeTableAsync(Table.GetQueryModel());
                }
            }
        }
    }
}
