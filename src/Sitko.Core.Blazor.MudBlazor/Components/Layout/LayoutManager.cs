using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using MudBlazor;
using Sitko.Core.App.Blazor.Layout;

namespace Sitko.Core.Blazor.MudBlazorComponents
{
    public class MudLayoutManager : BaseLayoutManager<MudLayoutData, MudLayoutOptions>
    {
        public MudLayoutManager(IOptionsMonitor<MudLayoutOptions> optionsMonitor) : base(optionsMonitor)
        {
        }
    }

    public class MudLayoutOptions : LayoutOptions
    {
    }

    public record MudLayoutData : LayoutData
    {
        public RenderFragment? Extra { get; init; }
        public List<BreadcrumbItem> Breadcrumbs { get; init; } = new();
    }
}
