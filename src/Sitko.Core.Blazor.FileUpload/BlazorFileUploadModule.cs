using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Tewr.Blazor.FileReader;

namespace Sitko.Core.Blazor.FileUpload
{
    public class BlazorFileUploadModule : BaseApplicationModule
    {
        public BlazorFileUploadModule(BaseApplicationModuleConfig config, Application application) : base(config,
            application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddFileReaderService();
        }
    }
}
