using FluentAssertions;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.Storage.Tests;

public abstract class BasicTests<T> : BaseTest<T>
    where T : IBaseTestScope
{
    protected BasicTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task UploadFile()
    {
        var scope = await GetScopeAsync();

        var storage = scope.GetService<IStorage>();

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

        var storage = scope.GetService<IStorage>();

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

        var downloaded = await storage.DownloadAsync(uploaded.FilePath);

        Assert.NotNull(downloaded);
        await using (downloaded)
        {
            Assert.Equal(fileLength, downloaded.StorageItem.FileSize);
            if (downloaded.Stream.CanSeek)
            {
                Assert.Equal(fileLength, downloaded.Stream.Length);
            }

            Assert.Equal(fileName, downloaded.StorageItem.FileName);
        }
    }

    [Fact]
    public async Task DeleteFile()
    {
        var scope = await GetScopeAsync();

        var storage = scope.GetService<IStorage>();

        Assert.NotNull(storage);

        StorageItem uploaded;
        const string fileName = "file.txt";
        await using (var file = File.Open("Data/file.txt", FileMode.Open))
        {
            uploaded = await storage.SaveAsync(file, fileName, "upload");
        }

        Assert.NotNull(uploaded);

        var result = await storage.DeleteAsync(uploaded.FilePath);

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteFileError()
    {
        var scope = await GetScopeAsync();

        var storage = scope.GetService<IStorage>();

        Assert.NotNull(storage);

        var result = await storage.DeleteAsync(Guid.NewGuid().ToString());

        Assert.False(result);
    }

    [Fact]
    public async Task Traverse()
    {
        var scope = await GetScopeAsync();

        var storage = scope.GetService<IStorage>();

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

    [Fact]
    public async Task TraverseAfterUpdate()
    {
        var scope = await GetScopeAsync();

        var storage = scope.GetService<IStorage>();

        Assert.NotNull(storage);

        StorageItem uploaded;
        const string fileName = "file.txt";
        var metaData = new FileMetaData();
        var dir = "upload/dir1/dir2";
        await using (var file = File.Open("Data/file.txt", FileMode.Open))
        {
            uploaded = await storage.SaveAsync(file, fileName, dir, metaData);
        }

        Assert.NotNull(uploaded);

        var content = await storage.GetDirectoryContentsAsync(dir);
        content.Should().HaveCount(1);

        const string fileName2 = "file2.txt";
        var metaData2 = new FileMetaData();
        await using (var file = File.Open("Data/file.txt", FileMode.Open))
        {
            uploaded = await storage.SaveAsync(file, fileName2, dir, metaData2);
        }

        uploaded.Should().NotBeNull();

        content = await storage.GetDirectoryContentsAsync(dir);
        content.Should().HaveCount(2);
    }

    protected static async Task CheckFoldersContent(IStorage storage, StorageItem uploaded,
        FileMetaData? metaData)
    {
        var uploadDirectoryContent = (await storage.GetDirectoryContentsAsync("upload")).ToArray();
        uploadDirectoryContent.Should().NotBeEmpty().And.ContainSingle();
        var first = uploadDirectoryContent.First();
        first.Should().NotBeNull().And.BeOfType<StorageNode>();
        first.Name.Should().Be("dir1");

        var dir1DirectoryContent = (await storage.GetDirectoryContentsAsync(first.FullPath)).ToList();
        dir1DirectoryContent.Should().NotBeEmpty().And.ContainSingle();
        var second = dir1DirectoryContent.First();
        second.Should().NotBeNull().And.BeOfType<StorageNode>();
        second.Name.Should().Be("dir2");

        var dir2DirectoryContent = (await storage.GetDirectoryContentsAsync(second.FullPath)).ToList();
        dir2DirectoryContent.Should().NotBeEmpty().And.ContainSingle();
        var fileNode = dir2DirectoryContent.First();
        fileNode.Should().NotBeNull();
        fileNode.Type.Should().Be(StorageNodeType.StorageItem);
        if (fileNode.StorageItem != null)
        {
            fileNode.FullPath.Should().Be(uploaded.FilePath);
            fileNode.Name.Should().Be(uploaded.FileName);

            if (metaData is not null)
            {
                var itemMetaData = fileNode.StorageItem.GetMetadata<FileMetaData>();
                itemMetaData.Should().NotBeNull();
                itemMetaData!.Id.Should().Be(metaData.Id);
            }
        }
    }

    [Fact]
    public async Task Metadata()
    {
        var scope = await GetScopeAsync();

        var storage = scope.GetService<IStorage>();

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

    [Fact]
    public async Task UpdateMetadata()
    {
        var scope = await GetScopeAsync();
        var storage = scope.GetService<IStorage>();
        StorageItem uploaded;
        const string fileName = "file.txt";
        var metaData = new FileMetaData();
        await using (var file = File.Open("Data/file.txt", FileMode.Open))
        {
            uploaded = await storage.SaveAsync(file, fileName, "upload/dir1/dir2", metaData);
        }

        Assert.NotNull(uploaded);

        var newFileName = "fileNew.txt";
        var newMetaData = new FileMetaData();

        await storage.UpdateMetaDataAsync(uploaded, newFileName, newMetaData);

        var newItem = await storage.GetAsync(uploaded.FilePath);
        Assert.NotNull(newItem);
        Assert.Equal(newFileName, newItem.FileName);
        var itemMetaData = newItem.GetMetadata<FileMetaData>();
        Assert.NotNull(itemMetaData);
        Assert.Equal(newMetaData.Id, itemMetaData.Id);
    }

    [Fact]
    public async Task UploadToRoot()
    {
        var scope = await GetScopeAsync();

        var storage = scope.GetService<IStorage>();

        Assert.NotNull(storage);

        StorageItem uploaded;
        const string fileName = "file.txt";
        const string path = "";
        await using (var file = File.Open("Data/file.txt", FileMode.Open))
        {
            uploaded = await storage.SaveAsync(file, fileName, path);
        }

        uploaded.Should().NotBeNull();
    }
}

public class FileMetaData
{
    public Guid Id { get; set; } = Guid.NewGuid();
}
