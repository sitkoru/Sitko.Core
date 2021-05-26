using System.IO;
using System.Threading.Tasks;
using Sitko.Core.Storage.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Storage.Metadata.Postgres.Tests
{
    public class BasicTests : BasicTests<BasePostgresStorageTestScope, TestS3StorageSettings>
    {
        public BasicTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task Refresh()
        {
            var scope = await GetScopeAsync();
            var storage = scope.Get<IStorage<TestS3StorageSettings>>();
            var metadataProvider = scope.Get<IStorageMetadataProvider<TestS3StorageSettings>>();


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

            uploaded.FileName = Path.GetFileName(uploaded.FilePath); // cause we lost all metadata

            await CheckFoldersContent(storage, uploaded, null);
        }
    }
}
