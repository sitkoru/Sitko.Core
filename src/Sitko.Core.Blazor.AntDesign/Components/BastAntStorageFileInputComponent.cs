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
        private IDisposable? thisReference;
        protected ElementReference Btn { get; set; }

        [Inject] private MessageService MessageService { get; set; } = null!;

        [Inject]
        protected ILocalizationProvider<BastAntStorageFileInputComponent<TInput>> LocalizationProvider { get; set; } =
            null!;

        [Inject] private IJSRuntime JsRuntime { get; set; } = null!;

        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Parameter] public string ButtonText { get; set; } = "";
        [Parameter] public string ListType { get; set; } = "text";

        [JSInvokable]
        public Task NotifyChange() => UploadFilesAsync();

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
            MessageService.Error(LocalizationProvider[
                "File {0} is too big — {1}. Files up to {2} are allowed.",
                fileName, FilesHelper.HumanSize(fileSize), FilesHelper.HumanSize(MaxFileSize)]);
            return Task.CompletedTask;
        }

        protected override Task NotifyFileContentTypeNotAllowedAsync(string fileName, string fileContentType)
        {
            MessageService.Error(LocalizationProvider[
                "File {0} content type {1} is not in allowed list. Allowed types: {2}",
                fileName, fileContentType, ContentTypes]);
            return Task.CompletedTask;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                thisReference = DotNetObjectReference.Create(this);
                await JsRuntime.InvokeVoidAsync("SitkoCoreBlazorAntDesign.FileUpload.init", InputRef, Btn,
                    thisReference);
            }
        }

        protected override Task NotifyUploadAsync(int resultsCount)
        {
            MessageService.Success(LocalizationProvider["{0} files were uploaded", resultsCount]);
            return Task.CompletedTask;
        }

        protected override Task NotifyMaxFilesCountExceededAsync(int filesCount)
        {
            MessageService.Error(LocalizationProvider["Maximum of {0} files is allowed. Selected: {1}",
                MaxAllowedFiles!,
                filesCount]);
            return Task.CompletedTask;
        }

        public virtual void Dispose()
        {
            thisReference?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
