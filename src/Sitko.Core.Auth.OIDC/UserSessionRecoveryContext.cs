namespace Sitko.Core.Auth.OIDC
{

    public static class UserSessionRecoveryContext
    {
        private static readonly AsyncLocal<Func<Task>?> Recovery = new();

        public static Task RequestRecoveryAsync() => Recovery.Value?.Invoke() ?? Task.CompletedTask;

        public static async Task RunAsync(Func<Task> operation, Func<Task> recover)
        {
            var previous = Recovery.Value;
            Recovery.Value = recover;
            try
            {
                await operation();
            }
            finally
            {
                Recovery.Value = previous;
            }
        }
    }
}
