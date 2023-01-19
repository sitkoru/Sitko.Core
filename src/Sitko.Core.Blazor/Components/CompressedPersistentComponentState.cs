using Microsoft.AspNetCore.Components;

namespace Sitko.Core.Blazor.Components;

public class CompressedPersistentComponentState : ICompressedPersistentComponentState, IDisposable
{
    private readonly PersistentComponentState persistentComponentState;
    private readonly IStateCompressor stateCompressor;

    private readonly List<PersistingComponentStateSubscription>
        subscriptions = new();

    public CompressedPersistentComponentState(PersistentComponentState persistentComponentState,
        IStateCompressor stateCompressor)
    {
        this.persistentComponentState = persistentComponentState;
        this.stateCompressor = stateCompressor;
    }

    public async Task PersistAsBytesAsync<T>(string key, T data) where T : notnull
    {
        var gzippedBytes = await stateCompressor.ToGzipAsync(data);
        persistentComponentState.PersistAsJson(key, gzippedBytes);
    }

    public async Task<(bool isSuccess, T? data)> TryTakeFromBytesAsync<T>(string key)
    {
        var data = default(T);
        if (persistentComponentState.TryTakeFromJson<byte[]>(key, out var gzippedBytes))
        {
            data = await stateCompressor.FromGzipAsync<T>(gzippedBytes!);
            return (true, data);
        }

        return (false, data);
    }


    public void RegisterOnPersisting(Func<Task> callback)
    {
        var subscription = persistentComponentState.RegisterOnPersisting(callback);
        subscriptions.Add(subscription);
    }

    public void Dispose()
    {
        foreach (var subscription in subscriptions)
        {
            subscription.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}

