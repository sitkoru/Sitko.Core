using Microsoft.AspNetCore.Components;

namespace Sitko.Core.Blazor.MudBlazorComponents.Helpers;

public static class NavigationManagerHelpers
{
    public static bool IsSubGroupExpanded(this NavigationManager navigationManager, string path)
    {
        var uri = new Uri(navigationManager.Uri);
        return uri.PathAndQuery.StartsWith(path, StringComparison.InvariantCulture);
    }
}

