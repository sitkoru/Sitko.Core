namespace Sitko.Core.Blazor.Components;

public interface IStateCompressor
{
    Task<byte[]> ToGzipAsync<T>(T value) where T : notnull;

    Task<T?> FromGzipAsync<T>(byte[] bytes);
}

