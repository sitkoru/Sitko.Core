using Microsoft.AspNetCore.Components;

namespace Sitko.Core.Blazor.Server
{

    public sealed class OidcSessionRecovery : ComponentBase, IDisposable
    {
        private bool recovering;

        [Inject] private OidcSessionRecoveryState State { get; set; } = null!;
        [Inject] private NavigationManager Navigation { get; set; } = null!;

        protected override void OnInitialized() => State.RecoveryRequired += RecoverAsync;

        private Task RecoverAsync() => InvokeAsync(() =>
        {
            if (recovering)
            {
                return;
            }

            recovering = true;
            Navigation.NavigateTo(Navigation.Uri, forceLoad: true);
        });

        public void Dispose() => State.RecoveryRequired -= RecoverAsync;
    }
}
