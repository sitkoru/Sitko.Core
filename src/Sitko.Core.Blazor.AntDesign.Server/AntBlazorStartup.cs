// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Hosting;
// using Sitko.Core.App.Localization;
// using Sitko.Core.Blazor.Forms;
// using Sitko.Core.Blazor.Server;
//
// namespace Sitko.Core.Blazor.AntDesign.Server;
//
// public class AntBlazorStartup : BlazorStartup
// {
//     public AntBlazorStartup(IConfiguration configuration, IHostEnvironment environment) : base(configuration,
//         environment)
//     {
//     }
//
//     protected override void ConfigureAppServices(IServiceCollection services)
//     {
//         base.ConfigureAppServices(services);
//         services.AddAntDesign();
//         services.Configure<JsonLocalizationModuleOptions>(options =>
//         {
//             options.AddDefaultResource(typeof(BaseForm));
//         });
//     }
// }
