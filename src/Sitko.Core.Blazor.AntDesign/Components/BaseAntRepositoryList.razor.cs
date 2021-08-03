using System.Threading.Tasks;
using AntDesign;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OneOf;
using Sitko.Core.App.Blazor.Forms;
using Sitko.Core.App.Localization;
using Sitko.Core.Repository;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AntDesign.TableModels;


    public class
        AntRepositoryList<TItem, TEntityPk> : BaseAntRepositoryList<TItem, TEntityPk, IRepository<TItem, TEntityPk>>
        where TItem : class, IEntity<TEntityPk>, new()
    {
    }

    public partial class BaseAntRepositoryList<TEntity, TEntityPk, TRepository>
        where TEntity : class, IEntity<TEntityPk>, new() where TRepository : IRepository<TEntity, TEntityPk>
    {
        [Parameter] public RenderFragment<TEntity>? ChildContent { get; set; }

        [Parameter] public RenderFragment<TEntity>? RowTemplate { get; set; }

        [Parameter] public RenderFragment<RowData<TEntity>>? ExpandTemplate { get; set; }

        [Parameter] public Func<RowData<TEntity>, bool> RowExpandable { get; set; } = _ => true;

        [Parameter] public Func<TEntity, IEnumerable<TEntity>> TreeChildren { get; set; } = _ => Enumerable.Empty<TEntity>();

        [Parameter]
        public Func<RowData<TEntity>, Dictionary<string, object>> OnRow { get; set; } =
            _ => new Dictionary<string, object>();

        [Parameter]
        public Func<Dictionary<string, object>> OnHeaderRow { get; set; } = () => new Dictionary<string, object>();

        [Parameter] public string? Title { get; set; }

        [Parameter] public RenderFragment? TitleTemplate { get; set; }

        [Parameter] public string? Footer { get; set; }

        [Parameter] public RenderFragment? FooterTemplate { get; set; }

        [Parameter] public TableSize? Size { get; set; }

        [Parameter] public TableLocale Locale { get; set; } = LocaleProvider.CurrentLocale.Table;

        [Parameter] public bool Bordered { get; set; }

        [Parameter] public string? ScrollX { get; set; }

        [Parameter] public string? ScrollY { get; set; }

        [Parameter] public int ScrollBarWidth { get; set; } = 17;

        [Parameter] public int IndentSize { get; set; } = 15;

        [Parameter] public int ExpandIconColumnIndex { get; set; }

        [Parameter] public Func<RowData<TEntity>, string> RowClassName { get; set; } = _ => "";

        [Parameter] public Func<RowData<TEntity>, string> ExpandedRowClassName { get; set; } = _ => "";

        [Parameter] public EventCallback<RowData<TEntity>> OnExpand { get; set; }

        [Parameter] public SortDirection[] SortDirections { get; set; } = SortDirection.Preset.Default;

        [Parameter] public string TableLayout { get; set; } = "";

        [Parameter] public EventCallback<RowData<TEntity>> OnRowClick { get; set; }

        [Parameter] public bool HidePagination { get; set; }

        [Parameter] public string PaginationPosition { get; set; } = "bottomRight";

        [Parameter] public EventCallback<int> TotalChanged { get; set; }

        [Parameter] public EventCallback<PaginationEventArgs> OnPageIndexChange { get; set; }

        [Parameter] public EventCallback<PaginationEventArgs> OnPageSizeChange { get; set; }

        [Parameter] public IEnumerable<TEntity>? SelectedRows { get; set; }

        [Parameter] public EventCallback<IEnumerable<TEntity>> SelectedRowsChanged { get; set; }
    }
}
