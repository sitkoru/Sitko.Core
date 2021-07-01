using System;

namespace Sitko.Core.Blazor.FileUpload
{
    public class FileUploadRequest
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public long Size { get; set; }
        public DateTime? LastModifiedDate { get; set; }
    }
}
