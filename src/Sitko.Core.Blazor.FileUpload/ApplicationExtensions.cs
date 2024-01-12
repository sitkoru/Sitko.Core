using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Blazor.FileUpload;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddBlazorFileUpload(this IHostApplicationBuilder hostApplicationBuilder)
    {
        hostApplicationBuilder.GetSitkoCore<ISitkoCoreBlazorApplicationBuilder>().AddBlazorFileUpload();
        return hostApplicationBuilder;
    }

    public static ISitkoCoreBlazorApplicationBuilder AddBlazorFileUpload(this ISitkoCoreBlazorApplicationBuilder applicationBuilder)
    {
        applicationBuilder.AddModule<BlazorFileUploadModule>();
        return applicationBuilder;
    }
}

