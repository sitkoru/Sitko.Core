using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Sitko.Core.App.Web.Razor
{
    public static class RazorExtensions
    {
        public static IServiceCollection AddViewToStringRenderer<TConfig>(this IServiceCollection services)
            where TConfig : IViewToStringRendererServiceOptions
        {
            services.AddHttpContextAccessor();
            services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.TryAddScoped<ViewToStringRendererService<TConfig>>();
            return services;
        }
    }
}
