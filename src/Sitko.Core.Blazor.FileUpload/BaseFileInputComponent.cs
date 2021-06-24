using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Sitko.Core.App.Blazor.Components;
using Tewr.Blazor.FileReader;

namespace Sitko.Core.Blazor.FileUpload
{
    public abstract class BaseFileInputComponent<TUploadResult> : BaseComponent where TUploadResult : IFileUploadResult
    {
        [Parameter] public string ContentTypes { get; set; } = "";
        [Parameter] public long MaxFileSize { get; set; }
        [Parameter] public int? MaxAllowedFiles { get; set; }
        [Parameter] public Func<IEnumerable<TUploadResult>, Task>? OnFilesUpload { get; set; }
        [Inject] private IFileReaderService FileReaderService { get; set; } = null!;
        public ElementReference InputRef;
        [CascadingParameter]
        public BaseComponent? Parent { get; set; }

        private static readonly string[] _units = {"bytes", "KB", "MB", "GB", "TB", "PB"};

        protected static string HumanSize(long fileSize)
        {
            if (fileSize < 1)
            {
                return "-";
            }

            var unit = 0;

            double size = fileSize;
            while (size >= 1024)
            {
                size /= 1024;
                unit++;
            }

            return $"{Math.Round(size, 2):N}{_units[unit]}";
        }

        protected async Task UploadFilesAsync()
        {
            await StartLoadingAsync();
            if (Parent is not null)
            {
                await Parent.NotifyStateChangeAsync();
            }
            var results = new List<TUploadResult>();
            var files = (await FileReaderService.CreateReference(InputRef).EnumerateFilesAsync()).ToArray();
            if (MaxAllowedFiles > 0 && files.Length > MaxAllowedFiles)
            {
                Logger.LogError("Max files count is {Count}", MaxAllowedFiles);
                await NotifyMaxFilesCountExceededAsync(files.Length);
                return;
            }

            foreach (var file in files)
            {
                var info = await file.ReadFileInfoAsync();
                try
                {
                    if (MaxFileSize > 0 && info.Size > MaxFileSize)
                    {
                        Logger.LogError("File {File} exceeds max file size of {Size}", info.Name, info.Size);
                        await NotifyFileExceedMaxSizeAsync(info.Name, info.Size);
                        continue;
                    }

                    if (ContentTypes.Any() && !ContentTypes.Split(',').Contains(info.Type))
                    {
                        Logger.LogError(
                            "File {File} content type {ContentType} is not in allowed list: {AllowedContentTypes}",
                            info.Name, info.Type, ContentTypes);
                        await NotifyFileContentTypeNotAllowedAsync(info.Name, info.Type);
                        continue;
                    }

                    var path = Path.GetTempFileName();

                    await using (FileStream fs = new(path, FileMode.Create))
                    {
                        await (await file.OpenReadAsync()).CopyToAsync(fs);
                        var request = new FileUploadRequest
                        {
                            Name = info.Name,
                            Type = info.Type,
                            Size = info.Size,
                            LastModifiedDate = info.LastModifiedDate,
                        };
                        results.Add(await SaveFileAsync(request, fs));
                    }

                    File.Delete(path);
                }
                catch (Exception ex)
                {
                    Logger.LogError("File: {Filename} Error: {Error}",
                        info.Name, ex.Message);
                }
            }

            if (results.Any())
            {
                Logger.LogDebug("Uploaded {Count} files", results.Count);
                await NotifyUploadAsync(results.Count);
                if (OnFilesUpload is not null)
                {
                    await OnFilesUpload(results);
                }
            }

            await StopLoadingAsync();
            if (Parent is not null)
            {
                await Parent.NotifyStateChangeAsync();
            }
        }


        protected abstract Task<TUploadResult> SaveFileAsync(FileUploadRequest file, FileStream stream);

        protected virtual Task NotifyMaxFilesCountExceededAsync(int filesCount)
        {
            return Task.CompletedTask;
        }

        protected virtual Task NotifyUploadAsync(int resultsCount)
        {
            return Task.CompletedTask;
        }


        protected virtual Task NotifyFileContentTypeNotAllowedAsync(string fileName, string fileContentType)
        {
            return Task.CompletedTask;
        }

        protected virtual Task NotifyFileExceedMaxSizeAsync(string fileName, long fileSize)
        {
            return Task.CompletedTask;
        }
    }
}
