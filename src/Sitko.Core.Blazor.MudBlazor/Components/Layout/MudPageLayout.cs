using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Sitko.Core.App.Blazor.Layout;

namespace Sitko.Core.Blazor.MudBlazorComponents;

public class MudPageLayout : BasePageLayout<MudLayoutData, MudLayoutOptions>
{
    [Parameter] public RenderFragment? Extra { get; set; }

    [Parameter] public List<BreadcrumbItem> Breadcrumbs { get; set; } = new();

    protected override MudLayoutData GetLayoutData() => new() { Title = Title, Description = Description, Breadcrumbs = Breadcrumbs, Extra = Extra };
}
