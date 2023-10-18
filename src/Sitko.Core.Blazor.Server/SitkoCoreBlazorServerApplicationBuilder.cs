using System.Reflection;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Blazor.ScriptInjector;
using Sitko.Core.App.Localization;
using Sitko.Core.App.Web;
using Sitko.Core.Blazor.Forms;

namespace Sitko.Core.Blazor.Server;

public class SitkoCoreBlazorServerApplicationBuilder : SitkoCoreWebApplicationBuilder,
    ISitkoCoreBlazorServerApplicationBuilder
{
    public SitkoCoreBlazorServerApplicationBuilder(WebApplicationBuilder builder, string[] args) : base(builder, args)
    {
        this.AddPersistentState();
        builder.Services.AddScriptInjector();
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents(options =>
            {
                options.DetailedErrors = builder.Environment.IsDevelopment();
            });
        builder.Services.Configure<JsonLocalizationModuleOptions>(options =>
        {
            options.AddDefaultResource(typeof(BaseForm));
        });
        if (Assembly.GetEntryAssembly() is { } assembly)
        {
            AddForms(assembly);
        }
    }

    [PublicAPI]
    public ISitkoCoreBlazorServerApplicationBuilder AddForms<TAssembly>() => AddForms(typeof(TAssembly).Assembly);

    [PublicAPI]
    public ISitkoCoreBlazorServerApplicationBuilder AddForms(Assembly assembly)
    {
         Services.Scan(s =>
            s.FromAssemblies(assembly).AddClasses(c => c.AssignableTo<BaseForm>()).AsSelf()
                .WithTransientLifetime());
         return this;
    }
}
