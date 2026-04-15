using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Sitko.Core.Blazor.Components;

namespace Sitko.Core.Blazor.FileUpload;

public interface IBaseFileInputComponent
{
    bool IsLoading { get; }
}

public abstract class BaseFileInputComponent<TUploadResult, TValue> : InputBase<TValue?>, IBaseFileInputComponent
    where TUploadResult : IFileUploadResult
{
    protected Guid InputFileKey { get; private set; } = Guid.NewGuid();
    [Parameter] public string ContentTypes { get; set; } = "";
    [Parameter] public long MaxFileSize { get; set; }
    [Parameter] public int? MaxAllowedFiles { get; set; }
    [Parameter] public Func<TValue?, Task>? OnChange { get; set; }

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

    // Теперь метод принимает события от InputFile
    protected async Task UploadFilesAsync(InputFileChangeEventArgs e)
    {
        await StartLoadingAsync();
        var results = new List<TUploadResult>();

        // Важно: GetMultipleFiles по умолчанию отдаст только 10 файлов.
        // Передаем ему наш лимит, либо int.MaxValue.
        var maxFilesLimit = MaxAllowedFiles > 0 ? MaxAllowedFiles.Value : int.MaxValue;

        // Получаем список файлов (IBrowserFile)
        var files = e.GetMultipleFiles(maxFilesLimit);

        if (MaxAllowedFiles > 0 && files.Count > MaxAllowedFiles)
        {
            Logger.LogError("Max files count is {Count}", MaxAllowedFiles);
            await NotifyMaxFilesCountExceededAsync(files.Count);
            await StopLoadingAsync();
            return;
        }

        foreach (var file in files)
        {
            try
            {
                // Свойства доступны напрямую, без ReadFileInfoAsync()
                if (MaxFileSize > 0 && file.Size > MaxFileSize)
                {
                    Logger.LogError("File {File} exceeds max file size of {Size}", file.Name, file.Size);
                    await NotifyFileExceedMaxSizeAsync(file.Name, file.Size);
                    continue;
                }

                if (!string.IsNullOrEmpty(ContentTypes) && !ContentTypes.Split(',').Contains(file.ContentType))
                {
                    Logger.LogError(
                        "File {File} content type {ContentType} is not in allowed list: {AllowedContentTypes}",
                        file.Name, file.ContentType, ContentTypes);
                    await NotifyFileContentTypeNotAllowedAsync(file.Name, file.ContentType);
                    continue;
                }

                var path = Path.GetTempFileName();

                await using (FileStream fs = new(path, FileMode.Create))
                {
                    // Важно: OpenReadStream также требует указать максимальный размер (по умолчанию 512 KB).
                    var maxReadSize = MaxFileSize > 0 ? MaxFileSize : 512000;

                    await using var stream = file.OpenReadStream(maxReadSize);
                    await stream.CopyToAsync(fs);

                    var request = new FileUploadRequest(file.Name, file.ContentType, file.Size, file.LastModified);
                    results.Add(await SaveFileAsync(request, fs));
                }

                File.Delete(path);
            }
            catch (Exception ex)
            {
                Logger.LogError("File: {Filename} Error: {Error}", file.Name, ex.Message);
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

        InputFileKey = Guid.NewGuid();
        await StopLoadingAsync();
    }

    protected abstract TValue? GetResult(IEnumerable<TUploadResult> results);
    protected abstract Task<TUploadResult> SaveFileAsync(FileUploadRequest file, FileStream stream);
    protected virtual Task NotifyMaxFilesCountExceededAsync(int filesCount) => Task.CompletedTask;
    protected virtual Task NotifyUploadAsync(int resultsCount) => Task.CompletedTask;
    protected virtual Task NotifyFileContentTypeNotAllowedAsync(string fileName, string fileContentType) => Task.CompletedTask;
    protected virtual Task NotifyFileExceedMaxSizeAsync(string fileName, long fileSize) => Task.CompletedTask;

    protected override bool TryParseValueFromString(string? value, out TValue result, out string validationErrorMessage)
    {
        result = default!;
        validationErrorMessage = "";
        return false;
    }
}
