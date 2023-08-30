namespace Sitko.Core.App.Abstractions;

public class CallbackDisposable : IDisposable
{
    private readonly Action callback;

    public CallbackDisposable(Action callback) => this.callback = callback;

    private bool isDisposed;

    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;
            callback();
            GC.SuppressFinalize(this);
        }
    }
}
