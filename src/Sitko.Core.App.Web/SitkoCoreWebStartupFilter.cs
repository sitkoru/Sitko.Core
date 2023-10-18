using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Sitko.Core.App.Web;

public class SitkoCoreWebStartupFilter : IStartupFilter
{
    private readonly IApplicationContext applicationContext;
    private readonly IReadOnlyList<IWebApplicationModule> webModules;

    public SitkoCoreWebStartupFilter(IApplicationContext applicationContext,
        IEnumerable<ApplicationModuleRegistration> applicationModuleRegistrations)
    {
        this.applicationContext = applicationContext;
        webModules =
            ModulesHelper.GetEnabledModuleRegistrations(applicationContext, applicationModuleRegistrations)
                .Select(r => r.GetInstance())
                .OfType<IWebApplicationModule>()
                .ToList();
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
        appBuilder =>
        {
            foreach (var webModule in webModules)
            {
                webModule.ConfigureAppBuilder(applicationContext, appBuilder);
            }

            next(appBuilder);
        };
}
