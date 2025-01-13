using System.Net.Mail;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Email;

public abstract class FluentEmailModule<TEmailModuleOptions> : EmailModule<TEmailModuleOptions>
    where TEmailModuleOptions : FluentEmailModuleOptions, new()
{
    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        TEmailModuleOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.AddScoped<HtmlRenderer>();
        services.AddScoped<IMailSender, FluentMailSender<TEmailModuleOptions>>();
        var address = new MailAddress(startupOptions.From);
        var builder = services.AddFluentEmail(address.Address, address.DisplayName);
        ConfigureBuilder(builder, startupOptions);
    }

    protected abstract void ConfigureBuilder(FluentEmailServicesBuilder builder,
        TEmailModuleOptions moduleOptions);
}

