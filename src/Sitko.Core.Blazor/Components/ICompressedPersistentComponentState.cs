namespace Sitko.Core.Blazor.Components;

public interface ICompressedPersistentComponentState
{
    Task PersistAsBytesAsync<T>(string key, T data) where T : notnull;
    Task<(bool isSuccess, T? data)> TryTakeFromBytesAsync<T>(string key);
    void RegisterOnPersisting(Func<Task> callback);
}

