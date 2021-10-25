using Microsoft.AspNetCore.Components;

namespace Sitko.Core.App.Blazor.Layout;

public abstract partial class BasePageLayout<TLayoutData, TLayoutOptions>
    where TLayoutData : LayoutData where TLayoutOptions : LayoutOptions
{
    [Parameter] public string Title { get; set; } = "";
    [Parameter] public string Description { get; set; } = "";

    [Inject] protected ILayoutManager<TLayoutData, TLayoutOptions> LayoutManager { get; set; } = null!;

    [Parameter] public RenderFragment ChildContent { get; set; } = null!;

    protected override void AfterInitialized()
    {
        base.AfterInitialized();
        LayoutManager.SetLayoutData(GetLayoutData());
    }

    protected abstract TLayoutData GetLayoutData();
}
