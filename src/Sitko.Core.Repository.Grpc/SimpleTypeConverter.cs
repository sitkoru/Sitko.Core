using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Sitko.Core.Repository.Grpc;

internal sealed class SimpleTypeConverter : JsonConverter
{
    public override bool CanRead => false;

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is Type type)
        {
            writer.WriteValue(type.FullName + ", " + type.Assembly.GetName().Name);
        }
        else
        {
            var t = JToken.FromObject(value!);
            t.WriteTo(writer);
        }
    }

    public override object
        ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) =>
        throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");

    public override bool CanConvert(Type objectType) => typeof(Type).IsAssignableFrom(objectType);
}

