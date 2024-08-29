using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using MudBlazor;
using Sitko.Core.App;

// ReSharper disable once CheckNamespace
namespace Sitko.Core.Blazor.MudBlazorComponents;

public abstract partial class BaseMudPageLayout
{
    [Parameter] public string Title { get; set; }

    [Parameter] public string Description { get; set; } = "";

    [Parameter] public RenderFragment ChildContent { get; set; }

    [Parameter] public RenderFragment? Extra { get; set; }

    [Parameter] public List<BreadcrumbItem> Breadcrumbs { get; set; } = new();
    protected virtual string PageTitleSeparator => "/";

    [Inject] private IOptionsMonitor<MudLayoutOptions> OptionsMonitor { get; set; } = null!;
    [Inject] private IApplicationContext ApplicationContext { get; set; } = null!;


    protected virtual string PageTitle
    {
        get
        {
            var prefix = $" {PageTitleSeparator} {ApplicationContext.Name}";
            if (!string.IsNullOrEmpty(OptionsMonitor.CurrentValue.PageTitlePostfix))
            {
                prefix = OptionsMonitor.CurrentValue.PageTitlePostfix;
            }

            return $"{Title}{prefix}";
        }
    }
}
