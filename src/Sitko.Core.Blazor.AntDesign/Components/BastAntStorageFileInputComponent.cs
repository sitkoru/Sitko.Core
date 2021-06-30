using System;
using System.Threading.Tasks;
using AntDesign;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Sitko.Core.App.Helpers;
using Sitko.Core.App.Localization;
using Sitko.Core.Blazor.FileUpload;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public abstract class BastAntStorageFileInputComponent<TInput> : BaseStorageFileInputComponent<TInput>, IDisposable
    {
        private IDisposable? _thisReference;
        protected ElementReference _btn;

        [Inject] private NotificationService NotificationService { get; set; } = null!;

        [Inject]
        protected ILocalizationProvider<BastAntStorageFileInputComponent<TInput>> LocalizationProvider { get; set; } =
            null!;

        [Inject] private IJSRuntime JsRuntime { get; set; } = null!;

        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Parameter] public string ButtonText { get; set; } = "";
        [Parameter] public string ListType { get; set; } = "text";

        [JSInvokable]
        public Task NotifyChange()
        {
            return UploadFilesAsync();
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            if (string.IsNullOrEmpty(ButtonText))
            {
                ButtonText = LocalizationProvider["Upload"];
            }
        }

        protected override Task NotifyFileExceedMaxSizeAsync(string fileName, long fileSize)
        {
            NotificationService.Error(new NotificationConfig
            {
                Message = LocalizationProvider["Error"],
                Description =
                    LocalizationProvider[
                        "File {0} is too big — {1}. Files up to {2} are allowed.",
                        fileName, FilesHelper.HumanSize(fileSize), FilesHelper.HumanSize(MaxFileSize)],
                Placement = NotificationPlacement.BottomRight
            });
            return Task.CompletedTask;
        }

        protected override Task NotifyFileContentTypeNotAllowedAsync(string fileName, string fileContentType)
        {
            NotificationService.Error(new NotificationConfig
            {
                Message = LocalizationProvider["Error"],
                Description =
                    LocalizationProvider["File {0} content type {1} is not in allowed list. Allowed types: {2}",
                        fileName, fileContentType, ContentTypes],
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
                Message = LocalizationProvider["Success"],
                Description = LocalizationProvider["{0} files were uploaded", resultsCount],
                Placement = NotificationPlacement.BottomRight
            });
            return Task.CompletedTask;
        }

        protected override Task NotifyMaxFilesCountExceededAsync(int filesCount)
        {
            NotificationService.Error(new NotificationConfig
            {
                Message = LocalizationProvider["Error"],
                Description = LocalizationProvider["Maximum of {0} files is allowed. Selected: {1}", MaxAllowedFiles!, filesCount],
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
