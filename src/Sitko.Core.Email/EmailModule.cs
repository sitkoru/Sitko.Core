using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.App.Web.Razor;

namespace Sitko.Core.Email;

public interface IEmailModule : IApplicationModule
{
}

public abstract class EmailModule<TEmailModuleOptions> : BaseApplicationModule<TEmailModuleOptions>, IEmailModule
    where TEmailModuleOptions : EmailModuleOptions, new()
{
    public override void ConfigureServices(IApplicationContext context, IServiceCollection services,
        TEmailModuleOptions startupOptions)
    {
        base.ConfigureServices(context, services, startupOptions);
        services.AddViewToStringRenderer<TEmailModuleOptions>();
    }
}
