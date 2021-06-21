using System;
using System.Collections.Generic;
using System.Linq;
using AntDesign;
using AntDesign.TableModels;
using Microsoft.AspNetCore.Components;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public partial class AntRepositoryList<TItem, TEntityPk>
        where TItem : class, Repository.IEntity<TEntityPk>, new()
    {
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

        [Parameter] public TableSize Size { get; set; }

        [Parameter] public TableLocale Locale { get; set; } = LocaleProvider.GetLocale("en-US").Table;

        [Parameter] public bool Bordered { get; set; } = false;

        [Parameter] public string? ScrollX { get; set; }

        [Parameter] public string? ScrollY { get; set; }

        [Parameter] public int ScrollBarWidth { get; set; } = 17;

        [Parameter] public int IndentSize { get; set; } = 15;

        [Parameter] public int ExpandIconColumnIndex { get; set; }

        [Parameter] public Func<RowData<TItem>, string> RowClassName { get; set; } = _ => "";

        [Parameter] public Func<RowData<TItem>, string> ExpandedRowClassName { get; set; } = _ => "";

        [Parameter] public EventCallback<RowData<TItem>> OnExpand { get; set; }

        [Parameter] public SortDirection[] SortDirections { get; set; } = SortDirection.Preset.Default;

        [Parameter] public string TableLayout { get; set; } = "";

        [Parameter] public EventCallback<RowData<TItem>> OnRowClick { get; set; }

        [Parameter] public bool HidePagination { get; set; }

        [Parameter] public string PaginationPosition { get; set; } = "bottomRight";

        [Parameter] public EventCallback<int> TotalChanged { get; set; }

        [Parameter] public EventCallback<PaginationEventArgs> OnPageIndexChange { get; set; }

        [Parameter] public EventCallback<PaginationEventArgs> OnPageSizeChange { get; set; }

        [Parameter] public IEnumerable<TItem> SelectedRows { get; set; }

        [Parameter] public EventCallback<IEnumerable<TItem>> SelectedRowsChanged { get; set; }

        protected Table<TItem> Table { get; set; }
    }
}
