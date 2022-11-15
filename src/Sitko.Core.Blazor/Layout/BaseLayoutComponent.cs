﻿using Microsoft.AspNetCore.Components;
using Sitko.Core.App;
using Sitko.Core.Blazor.Components;

namespace Sitko.Core.Blazor.Layout;

public abstract class BaseLayoutComponent<TLayoutData, TLayoutOptions> : BaseComponent
    where TLayoutData : LayoutData where TLayoutOptions : LayoutOptions
{
    protected string PageTitle { get; set; } = "";
    protected string Title { get; set; } = "";
    protected string Description { get; set; } = "";
    protected virtual string PageTitleSeparator => "/";
    [Inject] protected ILayoutManager<TLayoutData, TLayoutOptions> LayoutManager { get; set; } = null!;
    [Inject] protected IApplication Application { get; set; } = null!;

    [EditorRequired]
    [Parameter] public RenderFragment ChildContent { get; set; } = null!;

    protected override void Initialize()
    {
        base.Initialize();
        LayoutManager.OnDataChange += LayoutDataChanged;
    }

    private void LayoutDataChanged(TLayoutData layoutData)
    {
        Title = layoutData.Title;
        Description = layoutData.Description;
        var prefix = $" {PageTitleSeparator} {Application.Name}";
        if (!string.IsNullOrEmpty(LayoutManager.LayoutOptions.PageTitlePostfix))
        {
            prefix = LayoutManager.LayoutOptions.PageTitlePostfix;
        }

        PageTitle = $"{layoutData.Title}{prefix}";
        ProcessLayoutDataChange(layoutData);
        StateHasChanged();
    }

    protected virtual void ProcessLayoutDataChange(TLayoutData layoutData)
    {
    }
}
