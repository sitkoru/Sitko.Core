using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Sitko.Core.Blazor.Server
{

    public static class OidcSessionRecoveryExtensions
    {
        public static IServiceCollection AddOidcSessionRecoveryForBlazor(this IServiceCollection services)
        {
            services.TryAddScoped<OidcSessionRecoveryState>();
            services.TryAddEnumerable(ServiceDescriptor.Scoped<CircuitHandler, OidcSessionRecoveryCircuitHandler>());
            return services;
        }
    }
}
