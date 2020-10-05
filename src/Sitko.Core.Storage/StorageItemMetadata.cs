using System.Collections.Generic;

namespace Sitko.Core.Storage
{
    public class StorageItemMetadata
    {
        public const string FieldFileName = "filename";
        public const string FieldDate = "data";

        private readonly Dictionary<string, string> _metadata = new Dictionary<string, string>();

        public IEnumerable<KeyValuePair<string, string>> Metadata => _metadata;

        public StorageItemMetadata()
        {
        }

        public StorageItemMetadata(IEnumerable<KeyValuePair<string, string>> serializedData)
        {
            _metadata = new Dictionary<string, string>(serializedData);
        }

        public StorageItemMetadata Add(string key, string value)
        {
            _metadata[key.ToLowerInvariant()] = value;

            return this;
        }
    }
}
