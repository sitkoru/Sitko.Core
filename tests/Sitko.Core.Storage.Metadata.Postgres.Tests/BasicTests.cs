using Sitko.Core.Storage.Tests;
using Xunit;

namespace Sitko.Core.Storage.Metadata.Postgres.Tests;

public class BasicTests : BasicTests<BasePostgresStorageTestScope>
{
    public BasicTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Refresh()
    {
        var scope = await GetScopeAsync();
        var storage = scope.GetService<IStorage<TestS3StorageSettings>>();
        var metadataProvider = scope.GetService<IStorageMetadataProvider<TestS3StorageSettings>>();


        StorageItem uploaded;
        const string fileName = "file.txt";
        var metaData = new FileMetaData();
        await using (var file = File.Open("Data/file.txt", FileMode.Open))
        {
            uploaded = await storage.SaveAsync(file, fileName, "upload/dir1/dir2", metaData);
        }

        Assert.NotNull(uploaded);

        await metadataProvider.DeleteAllMetadataAsync();

        var uploadDirectoryContent = await storage.GetDirectoryContentsAsync("upload");
        Assert.Empty(uploadDirectoryContent);

        await storage.RefreshDirectoryContentsAsync("upload");

        var restored =
            new StorageItem(uploaded.FilePath)
            {
                FileName = Path.GetFileName(uploaded.FilePath)
            }; // cause we lost all metadata

        await CheckFoldersContent(storage, restored, null);
    }
}
