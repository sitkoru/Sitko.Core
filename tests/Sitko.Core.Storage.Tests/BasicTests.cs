using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Storage.Tests
{
    public abstract class BasicTests<T, TSettings> : BaseTest<T>
        where T : BaseTestScope where TSettings : StorageOptions
    {
        protected BasicTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task UploadFile()
        {
            var scope = await GetScopeAsync();

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
        }

        [Fact]
        public async Task DownloadFile()
        {
            var scope = await GetScopeAsync();

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
            Assert.NotNull(uploaded.FilePath);

            var downloaded = await storage.GetFileAsync(uploaded!.FilePath!);

            Assert.NotNull(downloaded);
            Assert.Equal(fileLength, downloaded?.FileSize);
        }

        [Fact]
        public async Task DeleteFile()
        {
            var scope = await GetScopeAsync();

            var storage = scope.Get<IStorage<TSettings>>();

            Assert.NotNull(storage);

            StorageItem uploaded;
            const string fileName = "file.txt";
            await using (var file = File.Open("Data/file.txt", FileMode.Open))
            {
                uploaded = await storage.SaveFileAsync(file, fileName, "upload");
            }

            Assert.NotNull(uploaded);

            var result = await storage.DeleteFileAsync(uploaded.FilePath!);

            Assert.True(result);
        }

        [Fact]
        public async Task DeleteFileError()
        {
            var scope = await GetScopeAsync();

            var storage = scope.Get<IStorage<TSettings>>();

            Assert.NotNull(storage);

            var result = await storage.DeleteFileAsync(Guid.NewGuid().ToString());

            Assert.False(result);
        }

        [Fact]
        public async Task Traverse()
        {
            var scope = await GetScopeAsync();

            var storage = scope.Get<IStorage<TSettings>>();

            Assert.NotNull(storage);

            StorageItem uploaded;
            const string fileName = "file.txt";
            await using (var file = File.Open("Data/file.txt", FileMode.Open))
            {
                uploaded = await storage.SaveFileAsync(file, fileName, "upload/dir1/dir2");
            }

            Assert.NotNull(uploaded);

            var uploadDirectoryContent = await storage.GetDirectoryContentsAsync("upload");
            Assert.NotEmpty(uploadDirectoryContent);
            Assert.Single(uploadDirectoryContent);
            var first = uploadDirectoryContent.First();
            Assert.NotNull(first);
            Assert.IsType<StorageFolder>(first);
            Assert.Equal("dir1", first.Name);

            var dir1DirectoryContent = await storage.GetDirectoryContentsAsync(first.FullPath);
            Assert.NotEmpty(dir1DirectoryContent);
            Assert.Single(dir1DirectoryContent);
            var second = dir1DirectoryContent.First();
            Assert.NotNull(second);
            Assert.IsType<StorageFolder>(second);
            Assert.Equal("dir2", second.Name);

            var dir2DirectoryContent = await storage.GetDirectoryContentsAsync(second.FullPath);
            Assert.NotEmpty(dir2DirectoryContent);
            Assert.Single(dir2DirectoryContent);
            var fileNode = dir2DirectoryContent.First();
            Assert.NotNull(fileNode);
            Assert.IsType<StorageItem>(fileNode);
            Assert.Equal(uploaded.FilePath, fileNode.FullPath);
        }
    }
}
