using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Sitko.Core.App.Web.Razor
{
    public static class RazorExtensions
    {
        public static IServiceCollection AddViewToStringRenderer(this IServiceCollection services, HostString host,
            string scheme)
        {
            services.AddHttpContextAccessor();
            services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.TryAddSingleton(new ViewToStringRendererServiceOptions(host,
                scheme));
            services.TryAddScoped<ViewToStringRendererService>();
            return services;
        }
    }
}
