namespace Sitko.Core.Blazor.Server
{

    internal sealed class OidcSessionRecoveryState
    {
        public event Func<Task>? RecoveryRequired;

        public Task RequestRecoveryAsync() => RecoveryRequired?.Invoke() ?? Task.CompletedTask;
    }
}
