using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using MudBlazor;
using Sitko.Core.App;
using Sitko.Core.Blazor.Layout;

// ReSharper disable once CheckNamespace
namespace Sitko.Core.Blazor.MudBlazorComponents;

public partial class BaseMudLayout
{
    [Parameter] public RenderFragment? Menu { get; set; }
    [Parameter] public RenderFragment ChildContent { get; set; } = null!;
    [Parameter] public string Logo { get; set; } = "";

    [Inject] private IApplicationContext ApplicationContext { get; set; } = null!;
    protected bool DrawerOpen { get; set; } = true;
    protected void DrawerToggle() => DrawerOpen = !DrawerOpen;
    [Inject] private IOptionsMonitor<MudLayoutOptions> OptionsMonitor { get; set; } = null!;

    protected virtual MudTheme Theme
    {
        get
        {
            switch (OptionsMonitor.CurrentValue.Theme)
            {
                case AppTheme.Light:
                    return new MudTheme();
                case AppTheme.Dark:
                    return new MudTheme
                    {
                        PaletteDark =
                        {
                            Primary = "#527cfa",
                            Secondary = "#b20942",
                            Black = "#27272f",
                            Background = "rgba(15,15,15, 1)",
                            BackgroundGray = "#27272f",
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
                            GrayLight = "rgba(35,35,35, 1)",
                            OverlayDark = "rgba(33,33,33, 0.9)"
                        }
                    };
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
