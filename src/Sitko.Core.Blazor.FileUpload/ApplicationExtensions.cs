using Sitko.Core.App;

namespace Sitko.Core.Blazor.FileUpload;

public static class ApplicationExtensions
{
    public static Application AddBlazorFileUpload(this Application application) =>
        application.AddModule<BlazorFileUploadModule>();
}

