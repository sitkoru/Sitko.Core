using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Tewr.Blazor.FileReader;

namespace Sitko.Core.Blazor.FileUpload
{
    public class BlazorFileUploadModule : BaseApplicationModule
    {
        public override string GetOptionsKey()
        {
            return "Blazor:FileUpload";
        }

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            BaseApplicationModuleOptions startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddFileReaderService();
        }
    }
}
