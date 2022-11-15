namespace Sitko.Core.Blazor.FileUpload;

public interface IFileUploadResult
{
    string FileName { get; }
    string FilePath { get; }
    long FileSize { get; }
    string Url { get; }
}

