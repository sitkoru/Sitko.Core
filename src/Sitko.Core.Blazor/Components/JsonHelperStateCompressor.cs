#if NET6_0_OR_GREATER
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using Sitko.Core.App.Json;

namespace Sitko.Core.Blazor.Components;

public class JsonHelperStateCompressor : IStateCompressor
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
