namespace Sitko.Core.Blazor.FileUpload;

public record FileUploadRequest(string Name, string ContentType, long Size, DateTimeOffset? LastModifiedDate);

