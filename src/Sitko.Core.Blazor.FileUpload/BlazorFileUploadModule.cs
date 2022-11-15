using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Tewr.Blazor.FileReader;

namespace Sitko.Core.Blazor.FileUpload;

public class BlazorFileUploadModule : BaseApplicationModule
{
    public override string OptionsKey => "Blazor:FileUpload";

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        BaseApplicationModuleOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.AddFileReaderService();
    }
}

