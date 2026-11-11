using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Sitko.Core.Auth.OIDC;

namespace Sitko.Core.Blazor.Server
{

    internal sealed class OidcSessionRecoveryCircuitHandler(NavigationManager navigation) : CircuitHandler
    {
        private int recovering;

        public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            _ = Interlocked.Exchange(ref recovering, 0);
            return Task.CompletedTask;
        }

        public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
            Func<CircuitInboundActivityContext, Task> next) =>
            context => UserSessionRecoveryContext.RunAsync(() => next(context), RequestRecoveryAsync);

        private Task RequestRecoveryAsync()
        {
            if (Interlocked.Exchange(ref recovering, 1) == 0)
            {
                navigation.Refresh(forceReload: true);
            }
            return Task.CompletedTask;
        }
    }
}
