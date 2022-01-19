using System.IO;
using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage.Internal;

public record UploadRequest(Stream Stream, string Path, string FileName,
    StorageItemMetadata? Metadata = null);
