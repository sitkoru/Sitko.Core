using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Sitko.Core.App.Blazor.Layout;

namespace Sitko.Core.Blazor.MudBlazorComponents;

public abstract class BaseMudLayout : BaseLayoutComponent<MudLayoutData, MudLayoutOptions>
{
    protected bool DrawerOpen { get; set; } = true;

    protected RenderFragment? Extra { get; private set; }

    protected List<BreadcrumbItem> Breadcrumbs { get; private set; } = new();

    protected void DrawerToggle() => DrawerOpen = !DrawerOpen;

    protected virtual MudTheme Theme
    {
        get
        {
            switch (LayoutManager.LayoutOptions.Theme)
            {
                case AppTheme.Light:
                    return new MudTheme();
                case AppTheme.Dark:
                    return new MudTheme
                    {
                        Palette = new Palette
                        {
                            Primary = "#527cfa",
                            Secondary = "#b20942",
                            Black = "#27272f",
                            Background = "rgba(15,15,15, 1)",
                            BackgroundGrey = "#27272f",
                            Surface = "rgba(30,30,31, 0.5)",
                            DrawerBackground = "rgba(22,22,23, 1)",
                            DrawerText = "rgba(255,255,255, 0.50)",
                            DrawerIcon = "rgba(255,255,255, 0.50)",
                            AppbarBackground = "rgba(22,22,23, 1)",
                            AppbarText = "rgba(255,255,255, 0.70)",
                            TextPrimary = "rgba(255,255,255, 0.70)",
                            TextSecondary = "rgba(255,255,255, 0.50)",
                            ActionDefault = "rgba(255,255,255, 1)",
                            ActionDisabled = "rgba(255,255,255, 0.26)",
                            ActionDisabledBackground = "rgba(255,255,255, 0.12)",
                            Divider = "rgba(255,255,255, 0.12)",
                            DividerLight = "rgba(255,255,255, 0.06)",
                            TableLines = "rgba(255,255,255, 0.12)",
                            LinesDefault = "rgba(255,255,255, 0.12)",
                            LinesInputs = "rgba(255,255,255, 0.3)",
                            TextDisabled = "rgba(255,255,255, 0.2)",
                            Info = "#3299ff",
                            Success = "#6faa09",
                            Warning = "#ffa800",
                            Error = "#f64e62",
                            Dark = "#27272f",
                            TableHover = "rgba(255,255,255, 0.03)",
                            TableStriped = "rgba(255,255,255, 0.02)",
                            GrayLight = "rgba(35,35,35, 1)"
                        }
                    };
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    protected override void ProcessLayoutDataChange(MudLayoutData layoutData)
    {
        base.ProcessLayoutDataChange(layoutData);
        Breadcrumbs = layoutData.Breadcrumbs;
        Extra = layoutData.Extra;
    }
}
