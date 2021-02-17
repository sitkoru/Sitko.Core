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
            const string path = "upload";
            await using (var file = File.Open("Data/file.txt", FileMode.Open))
            {
                uploaded = await storage.SaveAsync(file, fileName, path);
            }

            Assert.NotNull(uploaded);
            Assert.NotEqual(0, uploaded.FileSize);
            Assert.Equal(fileName, uploaded.FileName);
            Assert.Equal("text/plain", uploaded.MimeType);
            Assert.Equal(path, uploaded.Path);
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
                uploaded = await storage.SaveAsync(file, fileName, "upload");
            }

            Assert.NotNull(uploaded);
            Assert.NotNull(uploaded.FilePath);

            var downloaded = await storage.DownloadAsync(uploaded!.FilePath!);

            Assert.NotNull(downloaded);
            await using (downloaded)
            {
                Assert.Equal(fileLength, downloaded?.StorageItem.FileSize);
                Assert.Equal(fileLength, downloaded?.Stream.Length);
                Assert.Equal(fileName, downloaded?.StorageItem.FileName);
            }
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
                uploaded = await storage.SaveAsync(file, fileName, "upload");
            }

            Assert.NotNull(uploaded);

            var result = await storage.DeleteAsync(uploaded.FilePath!);

            Assert.True(result);
        }

        [Fact]
        public async Task DeleteFileError()
        {
            var scope = await GetScopeAsync();

            var storage = scope.Get<IStorage<TSettings>>();

            Assert.NotNull(storage);

            var result = await storage.DeleteAsync(Guid.NewGuid().ToString());

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
            var metaData = new FileMetaData();
            await using (var file = File.Open("Data/file.txt", FileMode.Open))
            {
                uploaded = await storage.SaveAsync(file, fileName, "upload/dir1/dir2", metaData);
            }

            Assert.NotNull(uploaded);

            await CheckFoldersContent(storage, uploaded, metaData);
        }

        protected static async Task CheckFoldersContent(IStorage<TSettings> storage, StorageItem uploaded,
            FileMetaData? metaData)
        {
            var uploadDirectoryContent = await storage.GetDirectoryContentsAsync("upload");
            Assert.NotEmpty(uploadDirectoryContent);
            Assert.Single(uploadDirectoryContent);
            var first = uploadDirectoryContent.First();
            Assert.NotNull(first);
            Assert.IsType<StorageNode>(first);
            Assert.Equal("dir1", first.Name);

            var dir1DirectoryContent = await storage.GetDirectoryContentsAsync(first.FullPath);
            Assert.NotEmpty(dir1DirectoryContent);
            Assert.Single(dir1DirectoryContent);
            var second = dir1DirectoryContent.First();
            Assert.NotNull(second);
            Assert.IsType<StorageNode>(second);
            Assert.Equal("dir2", second.Name);

            var dir2DirectoryContent = await storage.GetDirectoryContentsAsync(second.FullPath);
            Assert.NotEmpty(dir2DirectoryContent);
            Assert.Single(dir2DirectoryContent);
            var fileNode = dir2DirectoryContent.First();
            Assert.NotNull(fileNode);
            Assert.Equal(StorageNodeType.StorageItem, fileNode.Type);
            if (fileNode.StorageItem != null)
            {
                Assert.Equal(uploaded.FilePath, fileNode.FullPath);
                Assert.Equal(uploaded.FileName, fileNode.Name);

                if (metaData is not null)
                {
                    var itemMetaData = fileNode.StorageItem.GetMetadata<FileMetaData>();
                    Assert.NotNull(itemMetaData);
                    Assert.Equal(metaData.Id, itemMetaData.Id);
                }
            }
        }

        [Fact]
        public async Task Metadata()
        {
            var scope = await GetScopeAsync();

            var storage = scope.Get<IStorage<TSettings>>();

            Assert.NotNull(storage);

            StorageItem uploaded;
            const string fileName = "file.txt";
            var metaData = new FileMetaData();
            await using (var file = File.Open("Data/file.txt", FileMode.Open))
            {
                uploaded = await storage.SaveAsync(file, fileName, "upload/dir1/dir2", metaData);
            }

            Assert.NotNull(uploaded);

            var item = await storage.GetAsync(uploaded.FilePath);

            Assert.NotNull(item);

            var itemMetaData = item.GetMetadata<FileMetaData>();
            Assert.NotNull(itemMetaData);
            Assert.Equal(metaData.Id, itemMetaData.Id);
        }
    }

    public class FileMetaData
    {
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
