using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using SixLabors.ImageSharp.Web;
using SixLabors.ImageSharp.Web.Providers;
using SixLabors.ImageSharp.Web.Resolvers;

namespace Sitko.Core.Storage.Proxy.ImageSharp
{
    public abstract class ImageSharpStorageProvider : IImageProvider
    {
        protected static readonly char[] _slashChars = {'\\', '/'};
        private Func<HttpContext, bool>? _match;
        private readonly FormatUtilities _formatUtilities;

        protected ImageSharpStorageProvider(FormatUtilities formatUtilities)
        {
            _formatUtilities = formatUtilities;
        }

        public abstract Task<IImageResolver> GetAsync(HttpContext context);
        public ProcessingBehavior ProcessingBehavior => ProcessingBehavior.All;


        public Func<HttpContext, bool> Match
        {
            get => _match ?? IsMatch;
            set => _match = value;
        }

        private bool IsMatch(HttpContext context)
        {
            return true;
        }

        public bool IsValidRequest(HttpContext context)
            => _formatUtilities.GetExtensionFromUri(context.Request.GetDisplayUrl()) != null;
    }

    public class ImageSharpStorageProvider<TStorageOptions> : ImageSharpStorageProvider
        where TStorageOptions : StorageOptions
    {
        private readonly IStorage<TStorageOptions> _storage;

        public ImageSharpStorageProvider(IStorage<TStorageOptions> storage, FormatUtilities formatUtilities) : base(
            formatUtilities)
        {
            _storage = storage;
        }

        public override async Task<IImageResolver> GetAsync(HttpContext context)
        {
            var key = WebUtility.UrlDecode(context.Request.Path.Value).TrimStart(_slashChars);

            bool imageExists = await _storage.IsFileExistsAsync(key);

#pragma warning disable 8603
            return !imageExists ? null : new ImageSharpStorageResolver<TStorageOptions>(_storage, key);
#pragma warning restore 8603
        }
    }

    public class ImageSharpStorageResolver<TStorageOptions> : IImageResolver where TStorageOptions : StorageOptions
    {
        private readonly IStorage<TStorageOptions> _storage;
        private readonly string _imagePath;

        public ImageSharpStorageResolver(IStorage<TStorageOptions> storage, string key)
        {
            _storage = storage;
            _imagePath = key;
        }

        public async Task<ImageMetadata> GetMetaDataAsync()
        {
            var fileInfo = await _storage.GetFileAsync(_imagePath);
            if (fileInfo == null)
            {
                return new ImageMetadata(DateTime.UtcNow, 0);
            }

            return new ImageMetadata(fileInfo.LastModified.DateTime, fileInfo.FileSize);
        }

        public async Task<Stream> OpenReadAsync()
        {
            var file = await _storage.GetFileAsync(_imagePath);
#pragma warning disable 8603
            return file?.Stream;
#pragma warning restore 8603
        }
    }
}
