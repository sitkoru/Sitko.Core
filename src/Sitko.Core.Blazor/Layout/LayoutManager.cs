using Microsoft.Extensions.Options;

namespace Sitko.Core.Blazor.Layout;

public interface ILayoutManager<TLayoutData, out TLayoutOptions>
    where TLayoutData : LayoutData where TLayoutOptions : LayoutOptions
{
    TLayoutData LayoutData { get; }
    TLayoutOptions LayoutOptions { get; }
    void SetLayoutData(TLayoutData layoutData);
    event Action<TLayoutData>? OnDataChange;
}

public abstract class BaseLayoutManager<TLayoutData, TLayoutOptions> : ILayoutManager<TLayoutData, TLayoutOptions>
    where TLayoutData : LayoutData where TLayoutOptions : LayoutOptions
{
    private readonly IOptionsMonitor<TLayoutOptions> optionsMonitor;

    protected BaseLayoutManager(IOptionsMonitor<TLayoutOptions> optionsMonitor) =>
        this.optionsMonitor = optionsMonitor;

    public event Action<TLayoutData>? OnDataChange;

    public TLayoutOptions LayoutOptions => optionsMonitor.CurrentValue;

    public void SetLayoutData(TLayoutData layoutData)
    {
        LayoutData = layoutData;
        OnDataChange?.Invoke(layoutData);
    }

    public TLayoutData LayoutData { get; private set; } = null!;
}

public class LayoutOptions
{
    public string PageTitlePostfix { get; set; } = "";
    public AppTheme Theme { get; set; } = AppTheme.Light;
}

public enum AppTheme
{
    Light = 1,
    Dark = 2
}

public record LayoutData
{
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
}

