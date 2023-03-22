using Microsoft.Extensions.Configuration;
using Serilog.Events;

namespace Sitko.Core.App.Logging;

public class SerilogDynamicConfigurationProvider : ConfigurationProvider
{
    private const string DefaultCategory = "Default";
    public static readonly SerilogDynamicConfigurationProvider Instance = new();

    public void SetLevel(LogEventLevel level, string? category = null)
    {
        category ??= DefaultCategory;
        Set(GetKey(category), level.ToString());
        OnReload();
    }

    private static string GetKey(string category)
    {
        var key = "Serilog:MinimumLevel:";
        if (category != DefaultCategory)
        {
            key += "Override:";
        }

        key += category;
        return key;
    }

    public void ResetLevel(string? category = null)
    {
        category ??= DefaultCategory;
        Set(GetKey(category), null);
        OnReload();
    }
}
