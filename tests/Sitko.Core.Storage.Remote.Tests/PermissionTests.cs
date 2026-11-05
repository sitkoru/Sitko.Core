using System.Net;
using Microsoft.AspNetCore.Mvc;
using Sitko.Core.App.Results;
using Sitko.Core.Storage.FileSystem;
using Sitko.Core.Storage.Remote.Server;
using Sitko.Core.Storage.Tests;
using Sitko.Core.Xunit;
using Sitko.Core.Xunit.Web;
using Xunit;

namespace Sitko.Core.Storage.Remote.Tests
{
    public sealed class PermissionTests(ITestOutputHelper testOutputHelper)
        : BaseTest<PermissionStorageTestScope>(testOutputHelper)
    {
        [Theory]
        [InlineData("upload")]
        [InlineData("delete")]
        public async Task DeniedMutationPreservesAllStoredFiles(string operation)
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var scope = await GetScopeAsync();
            var storage = scope.ServerStorage;
            await using var original = new MemoryStream([0, 1, 127, 128, 255]);
            var target = await storage.SaveAsync(original, "existing.bin", "saved",
                new FileMetaData { Id = Guid.Parse("e116a633-81df-460e-a912-c5f0103525d1") }, cancellationToken);
            await using var sentinel = new MemoryStream([42, 24, 255]);
            _ = await storage.SaveAsync(sentinel, "sentinel.bin", "saved", cancellationToken: cancellationToken);
            var before = await ReadFilesAsync(scope.StoragePath, cancellationToken);
            Assert.Equal(new byte[] { 0, 1, 127, 128, 255 }, before["saved/existing.bin"]);
            Assert.Equal(new byte[] { 42, 24, 255 }, before["saved/sentinel.bin"]);

            using var client = scope.CreateClient();
            using var form = new MultipartFormDataContent();
            form.Add(new ByteArrayContent([9, 8, 7]), "File", "existing.bin");
            form.Add(new StringContent("existing.bin"), "FileName");
            form.Add(new StringContent("saved"), "Path");
            using var response = operation == "upload"
                ? await client.PostAsync("/Permissions", form, cancellationToken)
                : await client.DeleteAsync($"/Permissions?path={Uri.EscapeDataString(target.FilePath)}",
                    cancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal($"{operation}-denied-by-test", await response.Content.ReadAsStringAsync(cancellationToken));
            Assert.Equal(operation == "upload" ? 1 : 0, scope.Permissions.UploadCalls);
            Assert.Equal(operation == "delete" ? 1 : 0, scope.Permissions.DeleteCalls);
            Assert.Equal(operation == "upload" ? "saved" : target.FilePath, scope.Permissions.Path);
            if (operation == "upload")
            {
                Assert.Equal("existing.bin", scope.Permissions.FileName);
            }

            var after = await ReadFilesAsync(scope.StoragePath, cancellationToken);
            Assert.Equal(before.Keys.Order(), after.Keys.Order());
            foreach (var (path, bytes) in before)
            {
                Assert.Equal(bytes, after[path]);
            }
        }

        private static async Task<Dictionary<string, byte[]>> ReadFilesAsync(string directory,
            CancellationToken cancellationToken)
        {
            var files = new Dictionary<string, byte[]>();
            foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            {
                files.Add(System.IO.Path.GetRelativePath(directory, file),
                    await File.ReadAllBytesAsync(file, cancellationToken));
            }

            return files;
        }
    }

    public sealed class PermissionStorageTestScope : WebTestScope
    {
        public string StoragePath { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
        public StoragePermissionState Permissions { get; } = new();
        public IStorage<PermissionStorageOptions> ServerStorage =>
            Host!.Services.GetRequiredService<IStorage<PermissionStorageOptions>>();

        public HttpClient CreateClient() => Server!.CreateClient();

        protected override WebApplicationBuilder ConfigureWebApplication(WebApplicationBuilder webApplicationBuilder,
            string name)
        {
            _ = base.ConfigureWebApplication(webApplicationBuilder, name)
                .AddFileSystemStorage<PermissionStorageOptions>(options =>
                {
                    options.StoragePath = StoragePath;
                    options.PublicUri = new Uri(StoragePath + "/");
                    options.PreserveOriginalFileName = true;
                })
                .AddFileSystemStorageMetadata<PermissionStorageOptions>();
            _ = webApplicationBuilder.Services.AddSingleton(Permissions);
            return webApplicationBuilder;
        }

        protected override async Task OnAfterDisposeAsync()
        {
            try
            {
                await base.OnAfterDisposeAsync();
            }
            finally
            {
                if (Directory.Exists(StoragePath))
                {
                    Directory.Delete(StoragePath, true);
                }
            }
        }
    }

    public sealed class PermissionStorageOptions : StorageOptions, IFileSystemStorageOptions
    {
        public string StoragePath { get; set; } = "";
    }

    public sealed class StoragePermissionState
    {
        public int UploadCalls { get; set; }
        public int DeleteCalls { get; set; }
        public string? Path { get; set; }
        public string? FileName { get; set; }
    }

    [Route("/Permissions")]
    public sealed class PermissionController(
        IStorage<PermissionStorageOptions> storage,
        ILogger<PermissionController> logger,
        StoragePermissionState permissions)
        : BaseRemoteStorageController<PermissionStorageOptions, FileMetaData>(storage, logger)
    {
        protected override Task<IOperationResult> CanUploadAsync(UploadStorageItem<FileMetaData> uploadStorageItem,
            HttpRequest request)
        {
            permissions.UploadCalls++;
            permissions.Path = uploadStorageItem.Path;
            permissions.FileName = uploadStorageItem.FileName;
            return Task.FromResult<IOperationResult>(new OperationResult("upload-denied-by-test"));
        }

        protected override Task<IOperationResult> CanDeleteAsync(string? path, HttpRequest request)
        {
            permissions.DeleteCalls++;
            permissions.Path = path;
            return Task.FromResult<IOperationResult>(new OperationResult("delete-denied-by-test"));
        }
    }
}
