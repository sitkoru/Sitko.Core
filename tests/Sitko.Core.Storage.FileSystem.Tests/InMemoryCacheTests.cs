using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Storage.Cache;
using Sitko.Core.Storage.Tests;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.Storage.FileSystem.Tests
{
    public sealed class InMemoryCacheTests(ITestOutputHelper testOutputHelper)
        : BaseTest<InMemoryCacheFileSystemStorageTestScope>(testOutputHelper)
    {
        [Fact]
        public async Task ReadsPreserveNameMetadataAndBytesWithInMemoryCache()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var scope = await GetScopeAsync();
            var storage = scope.GetService<IStorage>();
            var metadata = new FileMetaData { Id = Guid.Parse("797429a2-3f9c-477d-9e39-38730d7dac5f") };
            await using var source = new MemoryStream([0, 1, 127, 128, 255]);
            var uploaded = await storage.SaveAsync(source, "original.txt", "upload", metadata, cancellationToken);
            var items = new List<StorageItem>();
            var contents = new List<byte[]>();

            for (var read = 0; read < 2; read++)
            {
                var item = await storage.GetAsync(uploaded.FilePath, cancellationToken);
                Assert.NotNull(item);
                items.Add(item);

                await using var downloaded = await storage.DownloadAsync(uploaded.FilePath, cancellationToken);
                Assert.NotNull(downloaded);
                items.Add(downloaded.StorageItem);
                await using var content = new MemoryStream();
                await downloaded.Stream.CopyToAsync(content, cancellationToken);
                contents.Add(content.ToArray());
            }

            Assert.Multiple(
                () => Assert.All(contents, content => Assert.Equal(new byte[] { 0, 1, 127, 128, 255 }, content)),
                () => Assert.All(items, item => Assert.Equal("original.txt", item.FileName)),
                () => Assert.All(items, item =>
                {
                    var actualMetadata = item.GetMetadata<FileMetaData>();
                    Assert.NotNull(actualMetadata);
                    Assert.Equal(Guid.Parse("797429a2-3f9c-477d-9e39-38730d7dac5f"), actualMetadata.Id);
                }));

            var updatedMetadata = new FileMetaData { Id = Guid.Parse("f4aef66e-bd34-45b8-b248-344d17ea2eb5") };
            var updated = await storage.UpdateMetaDataAsync(uploaded, "renamed.txt", updatedMetadata, cancellationToken);
            var reread = await storage.GetAsync(uploaded.FilePath, cancellationToken);
            Assert.NotNull(reread);
            await using var redownloaded = await storage.DownloadAsync(uploaded.FilePath, cancellationToken);
            Assert.NotNull(redownloaded);
            await using var updatedContent = new MemoryStream();
            await redownloaded.Stream.CopyToAsync(updatedContent, cancellationToken);

            Assert.Equal(new byte[] { 0, 1, 127, 128, 255 }, updatedContent.ToArray());
            Assert.All<StorageItem>([updated, reread, redownloaded.StorageItem], item =>
            {
                Assert.Equal("renamed.txt", item.FileName);
                var actualMetadata = item.GetMetadata<FileMetaData>();
                Assert.NotNull(actualMetadata);
                Assert.Equal(Guid.Parse("f4aef66e-bd34-45b8-b248-344d17ea2eb5"), actualMetadata.Id);
            });
        }
    }

    public sealed class InMemoryCacheFileSystemStorageTestScope : BaseFileSystemStorageTestScope
    {
        protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name)
        {
            var builder = base.ConfigureApplication(hostBuilder, name);
            _ = builder.GetSitkoCore()
                .AddModule<InMemoryStorageCacheModule<TestFileSystemStorageSettings>, InMemoryStorageCacheOptions>();
            return builder;
        }
    }
}
