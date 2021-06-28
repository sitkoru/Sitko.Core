using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Sitko.Core.Storage;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public abstract class BaseAntStorageImageInput<TValue> : BaseAntStorageInput<UploadedImage, TValue>
    {
        protected bool PreviewVisible;
        protected string PreviewTitle = string.Empty;
        protected string ImgUrl = string.Empty;

        [Parameter] public Func<StorageItem, ImageSize, string>? GeneratePreviewUrl { get; set; }
        [Parameter] public override string ContentTypes { get; set; } = "image/jpeg,image/png,image/svg+xml";
        [Parameter] public string Size { get; set; } = "default";
        [Parameter] public bool Avatar { get; set; }

        protected string AvatarSize => Size switch
        {
            "large" => "238",
            "small" => "46",
            _ => "86"
        };

        protected override UploadedImage CreateUploadedItem(StorageItem storageItem)
        {
            var url = Storage.PublicUri(storageItem).ToString();
            var urls = new Dictionary<ImageSize, string> {{ImageSize.Full, url}};
            if (GeneratePreviewUrl is not null)
            {
                urls[ImageSize.Large] = GeneratePreviewUrl(storageItem, ImageSize.Large);
                urls[ImageSize.Small] = GeneratePreviewUrl(storageItem, ImageSize.Small);
            }

            return new UploadedImage(storageItem, urls);
        }

        protected void PreviewFile(UploadedImage file)
        {
            PreviewVisible = true;
            PreviewTitle = file.StorageItem.FileName!;
            ImgUrl = file.LargePreviewUrl;
        }
    }

    public class UploadedImage : UploadedItem
    {
        private Dictionary<ImageSize, string> Urls { get; }
        public string SmallPreviewUrl => GetUrl(ImageSize.Small);
        public string LargePreviewUrl => GetUrl(ImageSize.Large);
        public override string Url => GetUrl(ImageSize.Full);

        public UploadedImage(StorageItem storageItem, Dictionary<ImageSize, string> urls) : base(storageItem)
        {
            Urls = urls;
        }

        private string GetUrl(ImageSize type)
        {
            if (Urls.ContainsKey(type))
            {
                return Urls[type];
            }

            return Urls[ImageSize.Full];
        }
    }

    public enum ImageSize
    {
        Full,
        Small,
        Large
    }
}
