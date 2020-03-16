using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Sitko.Core.App
{
    public interface IApplicationModuleRegistration
    {
        IApplicationModule CreateModule(IHostEnvironment environment, IConfiguration configuration,
            Application application, bool configure = true);
    }
}
