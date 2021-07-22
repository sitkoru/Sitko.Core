namespace Sitko.Core.Storage.FileSystem.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using global::Xunit;
    using global::Xunit.Abstractions;
    using JetBrains.Annotations;
    using Xunit;

    public class MultipleStorageTests : BaseTest<MultipleStorageTestsScope>
    {
        public MultipleStorageTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task Multiple()
        {
            var scope = await GetScopeAsync();
            var storages = scope.GetServices<IStorage>();
            Assert.NotEmpty(storages);
            Assert.Equal(3, storages.Count());
        }

        [Fact]
        public async Task Default()
        {
            var scope = await GetScopeAsync();
            var storage = scope.GetService<IStorage>();
            Assert.NotNull(storage);
            Assert.IsAssignableFrom<IStorage<MultipleStorageTestsOptionsSecond>>(storage);
        }

        [Fact]
        public async Task Specific()
        {
            var scope = await GetScopeAsync();
            var storage = scope.GetService<IStorage<MultipleStorageTestsOptionsThird>>();
            Assert.NotNull(storage);
        }
    }

    [UsedImplicitly]
    public class MultipleStorageTestsScope : BaseFileSystemStorageTestScope
    {
        protected override TestApplication ConfigureApplication(TestApplication application, string name)
        {
            base.ConfigureApplication(application, name);
            application.AddFileSystemStorage<MultipleStorageTestsOptionsSecond>(
                moduleOptions =>
                {
                    var folder = Path.GetTempPath() + "/" + Guid.NewGuid();
                    moduleOptions.PublicUri = new Uri(folder);
                    moduleOptions.StoragePath = folder;
                    moduleOptions.IsDefault = true;
                });
            application.AddFileSystemStorageMetadata<MultipleStorageTestsOptionsSecond>();
            application.AddFileSystemStorage<MultipleStorageTestsOptionsThird>(
                moduleOptions =>
                {
                    var folder = Path.GetTempPath() + "/" + Guid.NewGuid();
                    moduleOptions.PublicUri = new Uri(folder);
                    moduleOptions.StoragePath = folder;
                });
            application.AddFileSystemStorageMetadata<MultipleStorageTestsOptionsThird>();
            return application;
        }
    }

    public class MultipleStorageTestsOptionsSecond : StorageOptions, IFileSystemStorageOptions
    {
        public string StoragePath { get; set; } = "/tmp/storage/second";
    }

    public class MultipleStorageTestsOptionsThird : StorageOptions, IFileSystemStorageOptions
    {
        public string StoragePath { get; set; } = "/tmp/storage/third";
    }
}
