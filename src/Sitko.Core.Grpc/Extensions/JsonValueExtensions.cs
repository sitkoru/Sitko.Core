using System.Text.Json;

namespace Sitko.Core.Grpc.Extensions
{
    public static class JsonValueExtensions
    {
        private static readonly JsonSerializerOptions Settings = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static JsonValue ToJsonValue(this object obj)
        {
            return new JsonValue {Json = JsonSerializer.Serialize(obj, Settings)};
        }

        public static T GetValue<T>(this JsonValue val)
        {
            return JsonSerializer.Deserialize<T>(val.Json, Settings);
        }
    }
}
