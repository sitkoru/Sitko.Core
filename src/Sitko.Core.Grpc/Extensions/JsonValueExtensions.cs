using System.Text.Json;
using JetBrains.Annotations;

namespace Sitko.Core.Grpc.Extensions;

[PublicAPI]
public static class JsonValueExtensions
{
    private static readonly JsonSerializerOptions Settings = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static JsonValue ToJsonValue(this object obj) => new() { Json = JsonSerializer.Serialize(obj, Settings) };

    public static T? GetValue<T>(this JsonValue val) => JsonSerializer.Deserialize<T>(val.Json, Settings);
}

