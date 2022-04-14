#if NET6_0_OR_GREATER
using System;
using System.Threading.Tasks;

namespace Sitko.Core.Blazor.Components;

public interface ICompressedPersistentComponentState
{
    Task PersistAsBytesAsync<T>(string key, T data);
    Task<(bool isSuccess, T? data)> TryTakeFromBytesAsync<T>(string key);
    void RegisterOnPersisting(Func<Task> callback);
}
#endif
