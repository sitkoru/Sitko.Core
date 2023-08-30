namespace Sitko.Core.App.Abstractions;

public class AsyncCallbackDisposable : IAsyncDisposable
{
    private readonly Func<Task> callback;

    public AsyncCallbackDisposable(Func<Task> callback) => this.callback = callback;

    private bool isDisposed;

    public async ValueTask DisposeAsync()
    {
        if (!isDisposed)
        {
            isDisposed = true;
            await callback();
            GC.SuppressFinalize(this);
        }
    }
}
