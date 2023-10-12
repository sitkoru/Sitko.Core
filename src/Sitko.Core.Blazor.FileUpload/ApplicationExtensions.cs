using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Blazor.FileUpload;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddBlazorFileUpload(this IHostApplicationBuilder hostApplicationBuilder)
    {
        hostApplicationBuilder.AddSitkoCore().AddBlazorFileUpload();
        return hostApplicationBuilder;
    }

    public static SitkoCoreApplicationBuilder AddBlazorFileUpload(this SitkoCoreApplicationBuilder applicationBuilder) =>
        applicationBuilder.AddModule<BlazorFileUploadModule>();
}

