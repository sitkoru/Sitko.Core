using System;
using JetBrains.Annotations;

namespace Sitko.Core.Blazor.FileUpload;

[PublicAPI]
public record FileUploadRequest(string Name, string ContentType, long Size, DateTimeOffset? LastModifiedDate);
