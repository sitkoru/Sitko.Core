using System.Text.Json;

namespace Sitko.Core.Storage.Metadata;

public class StorageItemMetadata
{
    public string? FileName { get; set; }

    public string? Data { get; set; }

    public void SetData(object data) => Data = JsonSerializer.Serialize(data);

    public TData? GetData<TData>() where TData : class =>
        string.IsNullOrEmpty(Data) ? null : JsonSerializer.Deserialize<TData>(Data);
}

