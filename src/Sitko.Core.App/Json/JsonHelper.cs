using Newtonsoft.Json;
using Serilog;

namespace Sitko.Core.App.Json
{
    public class JsonHelper
    {
        private static JsonSerializerSettings GetJsonSettings(bool throwOnError, bool prettify = false) =>
            new()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                DateParseHandling = DateParseHandling.DateTimeOffset,
                TypeNameHandling = TypeNameHandling.Auto,
                MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead,
                Formatting = prettify ? Formatting.Indented : Formatting.None,
                Error = (_, e) =>
                {
                    if (!throwOnError)
                    {
                        Log.Logger.Error(e.ErrorContext.Error, "Error deserializing json content: {ErrorText}",
                            e.ErrorContext.Error.ToString());
                        e.ErrorContext.Handled = true;
                    }
                }
            };

        public static string SerializeWithMetadata(object obj, bool throwOnError = true, bool prettify = false) =>
            JsonConvert.SerializeObject(obj, GetJsonSettings(throwOnError, prettify));

        public static T? DeserializeWithMetadata<T>(string json, bool throwOnError = true, bool prettify = false) =>
            JsonConvert.DeserializeObject<T>(json, GetJsonSettings(throwOnError, prettify));

        public static T? Clone<T>(T? obj, bool throwOnError = true, bool prettify = false) where T : class =>
            obj is null
                ? null
                : DeserializeWithMetadata<T>(SerializeWithMetadata(obj, throwOnError, prettify), throwOnError)!;
    }
}
