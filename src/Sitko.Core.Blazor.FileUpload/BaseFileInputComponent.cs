using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Sitko.Core.Blazor.Components;
using Tewr.Blazor.FileReader;

namespace Sitko.Core.Blazor.FileUpload;

public interface IBaseFileInputComponent
{
    bool IsLoading { get; }
}

public abstract class BaseFileInputComponent<TUploadResult, TValue> : InputBase<TValue?>, IBaseFileInputComponent
    where TUploadResult : IFileUploadResult
{
    protected ElementReference InputRef { get; set; }
    [Parameter] public string ContentTypes { get; set; } = "";
    [Parameter] public long MaxFileSize { get; set; }

    [Parameter] public int? MaxAllowedFiles { get; set; }
    [Parameter] public Func<TValue?, Task>? OnChange { get; set; }

    [Inject] private IFileReaderService FileReaderService { get; set; } = null!;
    [Inject] private ILogger<BaseFileInputComponent<TUploadResult, TValue>> Logger { get; set; } = null!;
    [CascadingParameter] public IBaseComponent? Parent { get; set; }

    public bool IsLoading { get; private set; }

    private async Task StartLoadingAsync()
    {
        IsLoading = true;
        await InvokeAsync(StateHasChanged);
        if (Parent is not null)
        {
            await Parent.NotifyStateChangeAsync();
        }
    }

    private async Task StopLoadingAsync()
    {
        IsLoading = false;
        await InvokeAsync(StateHasChanged);
        if (Parent is not null)
        {
            await Parent.NotifyStateChangeAsync();
        }
    }

    protected async Task UploadFilesAsync()
    {
        await StartLoadingAsync();
        var results = new List<TUploadResult>();
        var files = (await FileReaderService.CreateReference(InputRef).EnumerateFilesAsync()).ToArray();
        if (MaxAllowedFiles > 0 && files.Length > MaxAllowedFiles)
        {
            Logger.LogError("Max files count is {Count}", MaxAllowedFiles);
            await NotifyMaxFilesCountExceededAsync(files.Length);
            await StopLoadingAsync();
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
                        LastModifiedDate = info.LastModifiedDate
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
            CurrentValue = GetResult(results);
            if (OnChange is not null)
            {
                await OnChange(CurrentValue);
            }
        }

        await StopLoadingAsync();
    }

    protected abstract TValue? GetResult(IEnumerable<TUploadResult> results);


    protected abstract Task<TUploadResult> SaveFileAsync(FileUploadRequest file, FileStream stream);

    protected virtual Task NotifyMaxFilesCountExceededAsync(int filesCount) => Task.CompletedTask;

    protected virtual Task NotifyUploadAsync(int resultsCount) => Task.CompletedTask;


    protected virtual Task NotifyFileContentTypeNotAllowedAsync(string fileName, string fileContentType) =>
        Task.CompletedTask;

    protected virtual Task NotifyFileExceedMaxSizeAsync(string fileName, long fileSize) => Task.CompletedTask;

    protected override bool TryParseValueFromString(string? value, out TValue result,
        out string validationErrorMessage)
    {
        result = default!;
        validationErrorMessage = "";
        return false;
    }
}
