#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sitko.Core.App.Json;

namespace Sitko.Core.Blazor.Components;

public abstract class BaseStateComponent<TState> : BaseComponent where TState : BaseComponentState, new()
{
    [Inject] private ICompressedPersistentComponentState ComponentState { get; set; } = null!;
    protected TState State { get; set; } = new();
    private string StateKey => $"{GetType().Name}";

    protected override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        ComponentState.RegisterOnPersisting(async () => { await ComponentState.PersistAsBytesAsync(StateKey, State); });

        var componentState = await ComponentState.TryTakeFromBytesAsync<TState>(StateKey);
        if (componentState.isSuccess)
        {
            State = componentState.data!;
        }
        else
        {
            State = await LoadStateAsync();
        }
    }

    protected abstract Task<TState> LoadStateAsync();
}

public abstract class BaseComponentState
{
}

public interface ICompressedPersistentComponentState
{
    Task PersistAsBytesAsync<T>(string key, T data);
    Task<(bool isSuccess, T? data)> TryTakeFromBytesAsync<T>(string key);
    void RegisterOnPersisting(Func<Task> callback);
}

public class CompressedPersistentComponentState : ICompressedPersistentComponentState, IDisposable
{
    private readonly IStateCompressor stateCompressor;
    private readonly PersistentComponentState persistentComponentState;

    private readonly List<PersistingComponentStateSubscription>
        subscriptions = new();

    public CompressedPersistentComponentState(PersistentComponentState persistentComponentState) =>
        this.persistentComponentState = persistentComponentState;

    public void Dispose()
    {
        foreach (var subscription in subscriptions)
        {
            subscription.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    public async Task PersistAsBytesAsync<T>(string key, T data)
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
}

public interface IStateCompressor
{
    Task<byte[]> ToGzipAsync<T>(T value);

    Task<T> FromGzipAsync<T>(byte[] bytes);
}

public class StateCompressor : IStateCompressor
{
    public async Task<byte[]> ToGzipAsync<T>(T value)
    {
        await using var input = new MemoryStream();
        var json = JsonHelper.SerializeWithMetadata(value);
        await using var output = new MemoryStream();
        await using var zipStream = new GZipStream(output, CompressionLevel.SmallestSize);
        await zipStream.WriteAsync(Encoding.UTF8.GetBytes(json));
        await zipStream.FlushAsync();
        var result = output.ToArray();
        return result;
    }

    public async Task<T> FromGzipAsync<T>(byte[] bytes)
    {
        await using var inputStream = new MemoryStream(bytes);
        await using var outputStream = new MemoryStream();
        await using var decompressor = new GZipStream(inputStream, CompressionMode.Decompress);
        await decompressor.CopyToAsync(outputStream);
        var json = Encoding.UTF8.GetString(outputStream.ToArray());
        return JsonHelper.DeserializeWithMetadata<T>(json);
    }
}


#endif
