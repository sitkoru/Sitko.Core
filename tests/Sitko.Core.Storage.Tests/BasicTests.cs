using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Storage.Tests
{
    public abstract class BasicTests<T, TSettings> : BaseTest<T>
        where T : BaseTestScope where TSettings : IStorageOptions
    {
        protected BasicTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task UploadFile()
        {
            var scope = GetScope();

            var storage = scope.Get<IStorage<TSettings>>();

            Assert.NotNull(storage);

            StorageItem uploaded;
            const string fileName = "file.txt";
            await using (var file = File.Open("Data/file.txt", FileMode.Open))
            {
                uploaded = await storage.SaveFileAsync(file, fileName, "upload");
            }

            Assert.NotNull(uploaded);
            Assert.NotEqual(0, uploaded.FileSize);
            Assert.Equal(fileName, uploaded.FileName);
            Assert.NotNull(uploaded.PublicUri);
            Assert.Null(uploaded.ImageInfo);
        }

        [Fact]
        public async Task DownloadFile()
        {
            var scope = GetScope();

            var storage = scope.Get<IStorage<TSettings>>();

            Assert.NotNull(storage);

            StorageItem uploaded;
            long fileLength;
            const string fileName = "file.txt";
            await using (var file = File.Open("Data/file.txt", FileMode.Open))
            {
                fileLength = file.Length;
                uploaded = await storage.SaveFileAsync(file, fileName, "upload");
            }

            Assert.NotNull(uploaded);

            var downloaded = await storage.DownloadFileAsync(uploaded);

            Assert.NotNull(downloaded);
            Assert.Equal(fileLength, downloaded.Length);
        }

        [Fact]
        public async Task DeleteFile()
        {
            var scope = GetScope();

            var storage = scope.Get<IStorage<TSettings>>();

            Assert.NotNull(storage);

            StorageItem uploaded;
            const string fileName = "file.txt";
            await using (var file = File.Open("Data/file.txt", FileMode.Open))
            {
                uploaded = await storage.SaveFileAsync(file, fileName, "upload");
            }

            Assert.NotNull(uploaded);

            var result = await storage.DeleteFileAsync(uploaded.FilePath);

            Assert.True(result);
        }

        [Fact]
        public async Task DeleteFileError()
        {
            var scope = GetScope();

            var storage = scope.Get<IStorage<TSettings>>();

            Assert.NotNull(storage);

            var result = await storage.DeleteFileAsync(Guid.NewGuid().ToString());

            Assert.False(result);
        }

        [Fact]
        public async Task UploadImage()
        {
            var scope = GetScope();

            var storage = scope.Get<IStorage<TSettings>>();

            Assert.NotNull(storage);

            StorageItem uploaded;
            const string fileName = "img.jpg";
            await using (var file = File.Open("Data/img.jpg", FileMode.Open))
            {
                uploaded = await storage.SaveImageAsync(file, fileName, "upload");
            }

            Assert.NotNull(uploaded);
            Assert.NotEqual(0, uploaded.FileSize);
            Assert.Equal(fileName, uploaded.FileName);
            Assert.NotNull(uploaded.PublicUri);
            Assert.NotNull(uploaded.ImageInfo);
            Assert.Empty(uploaded.ImageInfo.Thumbnails);
        }

        [Fact]
        public async Task UploadImageWithThumbnail()
        {
            var scope = GetScope();

            var storage = scope.Get<IStorage<TSettings>>();

            Assert.NotNull(storage);

            StorageItem uploaded;
            const string fileName = "img.jpg";
            await using (var file = File.Open("Data/img.jpg", FileMode.Open))
            {
                uploaded = await storage.SaveImageAsync(file, fileName, "upload",
                    new List<StorageImageSize> {new StorageImageSize(100, 100)});
            }

            Assert.NotNull(uploaded);
            Assert.NotNull(uploaded.ImageInfo);
            Assert.NotEmpty(uploaded.ImageInfo.Thumbnails);
            var thumb = uploaded.GetImageUriByWidth(100);
            Assert.NotNull(thumb);
        }
    }
}
