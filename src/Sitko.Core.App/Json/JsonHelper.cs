using Newtonsoft.Json;
using Serilog;

namespace Sitko.Core.App.Json
{
    public class JsonHelper
    {
        private static JsonSerializerSettings GetJsonSettings(bool throwOnError) =>
            new()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
                MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead,
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

        public static string SerializeWithMetadata(object obj, bool throwOnError = true) =>
            JsonConvert.SerializeObject(obj, GetJsonSettings(throwOnError));

        public static T? DeserializeWithMetadata<T>(string json, bool throwOnError = true) =>
            JsonConvert.DeserializeObject<T>(json, GetJsonSettings(throwOnError));

        public static T? Clone<T>(T? obj, bool throwOnError = true) where T : class =>
            obj is null ? null : DeserializeWithMetadata<T>(SerializeWithMetadata(obj, throwOnError), throwOnError)!;
    }
}
