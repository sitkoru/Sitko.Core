using Microsoft.AspNetCore.Components.Server.Circuits;
using Sitko.Core.Auth.OIDC;

namespace Sitko.Core.Blazor.Server
{

    internal sealed class OidcSessionRecoveryCircuitHandler(OidcSessionRecoveryState state) : CircuitHandler
    {
        public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
            Func<CircuitInboundActivityContext, Task> next) =>
            context => UserSessionRecoveryContext.RunAsync(() => next(context), state.RequestRecoveryAsync);
    }
}
