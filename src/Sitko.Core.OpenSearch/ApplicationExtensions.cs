using JetBrains.Annotations;
using Sitko.Core.App;

namespace Sitko.Core.OpenSearch;

[PublicAPI]
public static class ApplicationExtensions
{
    public static Application AddOpenSearchLogging(this Application application,
        Action<IApplicationContext, OpenSearchLoggingModuleOptions> configure, string? optionsKey = null) =>
        application.AddModule<OpenSearchLoggingModule, OpenSearchLoggingModuleOptions>(configure, optionsKey);

    public static Application AddOpenSearchLogging(this Application application,
        Action<OpenSearchLoggingModuleOptions>? configure = null, string? optionsKey = null) =>
        application.AddModule<OpenSearchLoggingModule, OpenSearchLoggingModuleOptions>(configure, optionsKey);
}
