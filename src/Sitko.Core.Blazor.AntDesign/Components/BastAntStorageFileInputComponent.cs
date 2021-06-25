using System;
using System.Threading.Tasks;
using AntDesign;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Sitko.Core.App.Helpers;
using Sitko.Core.Blazor.FileUpload;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public abstract class BastAntStorageFileInputComponent<TInput> : BaseStorageFileInputComponent<TInput>, IDisposable
    {
        private IDisposable? _thisReference;
        protected ElementReference _btn;

        [Inject] private NotificationService NotificationService { get; set; } = null!;
        [Inject] private IJSRuntime JsRuntime { get; set; } = null!;

        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Parameter] public string ButtonText { get; set; } = "Upload";
        [Parameter] public string ListType { get; set; } = "text";

        [JSInvokable]
        public Task NotifyChange()
        {
            return UploadFilesAsync();
        }

        protected override Task NotifyFileExceedMaxSizeAsync(string fileName, long fileSize)
        {
            NotificationService.Error(new NotificationConfig
            {
                Message = "Error",
                Description =
                    $"File {fileName} is too big — {FilesHelper.HumanSize(fileSize)}. Files up to {FilesHelper.HumanSize(MaxFileSize)} are allowed.",
                Placement = NotificationPlacement.BottomRight
            });
            return Task.CompletedTask;
        }

        protected override Task NotifyFileContentTypeNotAllowedAsync(string fileName, string fileContentType)
        {
            NotificationService.Error(new NotificationConfig
            {
                Message = "Error",
                Description =
                    $"File {fileName} content type {fileContentType} is not in allowed list. Allowed types: {ContentTypes}",
                Placement = NotificationPlacement.BottomRight
            });
            return Task.CompletedTask;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _thisReference = DotNetObjectReference.Create(this);
                await JsRuntime.InvokeVoidAsync("SitkoCoreBlazorAntDesign.FileUpload.init", InputRef, _btn,
                    _thisReference);
            }
        }

        protected override Task NotifyUploadAsync(int resultsCount)
        {
            NotificationService.Success(new NotificationConfig
            {
                Message = "Success",
                Description = $"{resultsCount} files were uploaded",
                Placement = NotificationPlacement.BottomRight
            });
            return Task.CompletedTask;
        }
        
        protected override Task NotifyMaxFilesCountExceededAsync(int filesCount)
        {
            NotificationService.Error(new NotificationConfig
            {
                Message = "Error",
                Description = $"Maximum of {MaxAllowedFiles} files is allowed. Selected: {filesCount}",
                Placement = NotificationPlacement.BottomRight
            });
            return Task.CompletedTask;
        }

        public virtual void Dispose()
        {
            _thisReference?.Dispose();
        }
    }
}
