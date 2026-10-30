using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Storage.FileSystem;
using Sitko.Core.Storage.Metadata.Postgres.DB;
using Sitko.Core.Storage.Tests;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.Storage.Metadata.Postgres.Tests
{
    public sealed class StorageNameIsolationTests(ITestOutputHelper testOutputHelper)
        : BaseTest<StorageNameIsolationTestScope>(testOutputHelper)
    {
        [Fact]
        public async Task SamePathKeepsEachStorageNameAndMetadataInSharedDatabase()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var scope = await GetScopeAsync();
            var first = scope.GetService<IStorage<FirstMetadataStorageOptions>>();
            var second = scope.GetService<IStorage<SecondMetadataStorageOptions>>();
            await using var firstContent = new MemoryStream([1, 2, 3]);
            var firstItem = await first.SaveAsync(firstContent, "shared.bin", "directory",
                cancellationToken: cancellationToken);
            await using var secondContent = new MemoryStream([4, 5, 6]);
            var secondItem = await second.SaveAsync(secondContent, "shared.bin", "directory",
                cancellationToken: cancellationToken);
            Assert.Equal("directory/shared.bin", firstItem.FilePath);
            Assert.Equal(firstItem.FilePath, secondItem.FilePath);

            // Имена в метаданных различаются, физический путь остаётся одинаковым.
            _ = await first.UpdateMetaDataAsync(firstItem, "first-original.txt",
                new FileMetaData { Id = Guid.Parse("dd7b9222-b1af-4434-8e90-cf839be22932") }, cancellationToken);
            _ = await second.UpdateMetaDataAsync(secondItem, "second-original.txt",
                new FileMetaData { Id = Guid.Parse("ba5cc5ac-209d-4e65-9448-16b8537a103c") }, cancellationToken);

            var actualFirst = await first.GetAsync("directory/shared.bin", cancellationToken);
            var actualSecond = await second.GetAsync("directory/shared.bin", cancellationToken);
            Assert.NotNull(actualFirst);
            Assert.NotNull(actualSecond);
            Assert.Equal("first-original.txt", actualFirst.FileName);
            Assert.Equal("second-original.txt", actualSecond.FileName);
            Assert.Equal(Guid.Parse("dd7b9222-b1af-4434-8e90-cf839be22932"),
                actualFirst.GetMetadata<FileMetaData>()?.Id);
            Assert.Equal(Guid.Parse("ba5cc5ac-209d-4e65-9448-16b8537a103c"),
                actualSecond.GetMetadata<FileMetaData>()?.Id);

            var firstMetadata = await scope.GetService<IStorageMetadataProvider<FirstMetadataStorageOptions>>()
                .GetMetadataAsync("directory/shared.bin", cancellationToken);
            var secondMetadata = await scope.GetService<IStorageMetadataProvider<SecondMetadataStorageOptions>>()
                .GetMetadataAsync("directory/shared.bin", cancellationToken);
            Assert.NotNull(firstMetadata);
            Assert.NotNull(secondMetadata);
            Assert.Equal("first-original.txt", firstMetadata.FileName);
            Assert.Equal("second-original.txt", secondMetadata.FileName);
            Assert.Equal(Guid.Parse("dd7b9222-b1af-4434-8e90-cf839be22932"),
                firstMetadata.GetData<FileMetaData>()?.Id);
            Assert.Equal(Guid.Parse("ba5cc5ac-209d-4e65-9448-16b8537a103c"),
                secondMetadata.GetData<FileMetaData>()?.Id);

            await using var db = await scope.GetService<IDbContextFactory<StorageDbContext>>()
                .CreateDbContextAsync(cancellationToken);
            Assert.Equal(scope.DatabaseName, db.Database.GetDbConnection().Database);
            var storageNames = await db.Records.Where(static record => record.FilePath == "directory/shared.bin")
                .Select(static record => record.Storage).OrderBy(static storageName => storageName)
                .ToArrayAsync(cancellationToken);
            Assert.Collection(storageNames,
                static storageName => Assert.Equal("storage-a", storageName),
                static storageName => Assert.Equal("storage-b", storageName));
        }
    }

    public sealed class StorageNameIsolationTestScope : BaseTestScope
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        public string DatabaseName { get; } = "storage_isolation_" + Guid.NewGuid().ToString("N");

        protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name)
        {
            _ = base.ConfigureApplication(hostBuilder, name);
            var configuration = new Dictionary<string, string?>();
            foreach (var storageType in new[] { typeof(FirstMetadataStorageOptions), typeof(SecondMetadataStorageOptions) })
            {
                var prefix = $"Storage:Metadata:Postgres:{storageType.Name}";
                foreach (var key in new[] { "Host", "Port", "Username", "Password" })
                {
                    var value = hostBuilder.Configuration[$"Storage:Metadata:Postgres:TestS3StorageSettings:{key}"];
                    if (value is not null)
                    {
                        configuration[$"{prefix}:{key}"] = value;
                    }
                }

                configuration[$"{prefix}:Database"] = DatabaseName;
            }

            _ = hostBuilder.Configuration.AddInMemoryCollection(configuration);
            _ = hostBuilder
                .AddFileSystemStorage<FirstMetadataStorageOptions>(options =>
                {
                    options.Name = "storage-a";
                    options.StoragePath = Path.Combine(directory, "first");
                    options.PublicUri = new Uri(options.StoragePath + "/");
                    options.PreserveOriginalFileName = true;
                })
                .AddFileSystemStorage<SecondMetadataStorageOptions>(options =>
                {
                    options.Name = "storage-b";
                    options.StoragePath = Path.Combine(directory, "second");
                    options.PublicUri = new Uri(options.StoragePath + "/");
                    options.PreserveOriginalFileName = true;
                })
                .AddPostgresStorageMetadata<FirstMetadataStorageOptions>();
            _ = hostBuilder.GetSitkoCore()
                .AddModule<PostgresStorageMetadataModule<SecondMetadataStorageOptions>,
                    PostgresStorageMetadataModuleOptions<SecondMetadataStorageOptions>>();
            return hostBuilder;
        }

        protected override async Task OnDisposeAsync()
        {
            try
            {
                await base.OnDisposeAsync();
            }
            finally
            {
                await using var db = await GetService<IDbContextFactory<StorageDbContext>>().CreateDbContextAsync();
                _ = await db.Database.EnsureDeletedAsync();
            }
        }

        protected override async Task OnAfterDisposeAsync()
        {
            try
            {
                await base.OnAfterDisposeAsync();
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }
    }

    public sealed class FirstMetadataStorageOptions : StorageOptions, IFileSystemStorageOptions
    {
        public string StoragePath { get; set; } = "";
    }

    public sealed class SecondMetadataStorageOptions : StorageOptions, IFileSystemStorageOptions
    {
        public string StoragePath { get; set; } = "";
    }
}
