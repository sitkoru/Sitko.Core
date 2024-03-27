namespace Sitko.Core.App.Abstractions;

public class EmptyDisposable : IDisposable
{
    public static readonly IDisposable Instance = new EmptyDisposable();

    public void Dispose() => GC.SuppressFinalize(this);
}
