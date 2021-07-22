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
    }
}
