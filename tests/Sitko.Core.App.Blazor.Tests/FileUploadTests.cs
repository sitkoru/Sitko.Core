using System.Linq.Expressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor;
using Sitko.Core.Blazor.FileUpload;
using Sitko.Core.Blazor.MudBlazor.Server;
using Sitko.Core.Blazor.MudBlazorComponents;
using Sitko.Core.Blazor.Server;
using Sitko.Core.Storage;
using Sitko.Core.Storage.FileSystem;
using Xunit;

namespace Sitko.Core.App.Blazor.Tests
{

    public class FileUploadTests
    {
        private static readonly byte[] KnownBytes = [0, 1, 127, 128, 255];

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InputFileChangeUploadsExactBytesToFileSystemStorage(bool useMudBlazor)
        {
            var root = Path.Combine(Path.GetTempPath(), $"sitko-file-upload-{Guid.NewGuid():N}");
            _ = Directory.CreateDirectory(root);
            try
            {
                var components = new CapturingComponentActivator();
                await using var app = CreateApp(root, components);
                await using var scope = app.Services.CreateAsyncScope();
                InitializeNavigation(scope.ServiceProvider);
                await using var renderer = new HtmlRenderer(scope.ServiceProvider,
                    scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
                var storage = scope.ServiceProvider.GetRequiredService<IStorage>();
                StorageItem? uploaded = null;

                await RenderUploadAsync(renderer, components, storage, useMudBlazor, item => uploaded = item);
                await ChangeFilesAsync(renderer, components, new BrowserFile("known.txt", KnownBytes));

                Assert.NotNull(uploaded);
                Assert.Equal("known.txt", uploaded.FileName);
                await AssertDownloadedBytesAsync(storage, uploaded, KnownBytes);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TooManyFilesReportsErrorAndNextChangeStillUploads(bool useMudBlazor)
        {
            var root = Path.Combine(Path.GetTempPath(), $"sitko-file-upload-{Guid.NewGuid():N}");
            _ = Directory.CreateDirectory(root);
            try
            {
                var components = new CapturingComponentActivator();
                await using var app = CreateApp(root, components);
                await using var scope = app.Services.CreateAsyncScope();
                InitializeNavigation(scope.ServiceProvider);
                await using var renderer = new HtmlRenderer(scope.ServiceProvider,
                    scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
                var storage = scope.ServiceProvider.GetRequiredService<IStorage>();
                StorageItem? uploaded = null;
                var changeCount = 0;

                await RenderUploadAsync(renderer, components, storage, useMudBlazor, item =>
                {
                    uploaded = item;
                    changeCount++;
                });

                var overflow = await Record.ExceptionAsync(() => ChangeFilesAsync(renderer, components,
                    new BrowserFile("first.txt", KnownBytes), new BrowserFile("second.txt", KnownBytes)));
                var filesAfterRejection = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).ToArray();
                var changesAfterRejection = changeCount;
                var uploadedAfterRejection = uploaded;
                var errors = useMudBlazor
                    ? scope.ServiceProvider.GetRequiredService<ISnackbar>().ShownSnackbars
                        .Where(snackbar => snackbar.Severity == Severity.Error).Select(snackbar => snackbar.Message).ToArray()
                    : [.. components.Single<StorageItemsInput>().Errors];
                var stillLoading = useMudBlazor
                    ? components.Single<MudFilesUpload<List<StorageItem>>>().IsLoading
                    : components.Single<StorageItemsInput>().IsLoading;

                await ChangeFilesAsync(renderer, components, new BrowserFile("retry.txt", KnownBytes));
                Assert.Null(uploadedAfterRejection);
                Assert.Equal(0, changesAfterRejection);
                Assert.Empty(filesAfterRejection);
                Assert.Equal(1, changeCount);
                Assert.NotNull(uploaded);
                Assert.Equal("retry.txt", uploaded.FileName);
                await AssertDownloadedBytesAsync(storage, uploaded, KnownBytes);
                Assert.Null(overflow);
                Assert.Equal("Maximum of 1 files is allowed. Selected: 2", Assert.Single(errors));
                Assert.False(stillLoading);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        private static WebApplication CreateApp(string root, CapturingComponentActivator components)
        {
            var builder = WebApplication.CreateBuilder();
            var core = builder.AddSitkoCoreBlazorServer();
            _ = core.AddBlazorFileUpload();
            _ = core.AddMudBlazorServer();
            _ = core.AddFileSystemStorage<FileUploadStorageOptions>(options =>
            {
                options.StoragePath = root;
                options.PublicUri = new Uri("https://example.test/files/");
                options.IsDefault = true;
            });
            _ = builder.Services.AddSingleton<IComponentActivator>(components);
            return builder.Build();
        }

        private static void InitializeNavigation(IServiceProvider services)
        {
            var navigation = services.GetRequiredService<NavigationManager>();
            ((IHostEnvironmentNavigationManager)navigation).Initialize("http://localhost/", "http://localhost/");
        }

        private static async Task RenderUploadAsync(HtmlRenderer renderer, CapturingComponentActivator components,
            IStorage storage, bool useMudBlazor, Action<StorageItem> onChange)
        {
            if (useMudBlazor)
            {
                var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(MudFilesUpload<>.Storage)] = storage,
                    [nameof(MudFilesUpload<>.MaxAllowedFiles)] = 1,
                    [nameof(MudFilesUpload<>.ValueChanged)] =
                        EventCallback.Factory.Create<List<StorageItem>?>(renderer,
                            items =>
                            {
                                Assert.NotNull(items);
                                onChange(Assert.Single(items));
                            })
                });
                _ = await renderer.Dispatcher.InvokeAsync(() =>
                    renderer.RenderComponentAsync<MudFilesUpload<List<StorageItem>>>(parameters));
            }
            else
            {
                IEnumerable<StorageItem>? value = null;
                Expression<Func<IEnumerable<StorageItem>?>> valueExpression = () => value;
                var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(StorageItemsInput.Storage)] = storage,
                    [nameof(StorageItemsInput.MaxAllowedFiles)] = 1,
                    [nameof(StorageItemsInput.ValueExpression)] = valueExpression,
                    [nameof(StorageItemsInput.OnChange)] = (Func<IEnumerable<StorageItem>?, Task>)(items =>
                    {
                        Assert.NotNull(items);
                        onChange(Assert.Single(items));
                        return Task.CompletedTask;
                    })
                });
                _ = await renderer.Dispatcher.InvokeAsync(() => renderer.RenderComponentAsync<StorageItemsInput>(parameters));
            }

            Assert.NotEmpty(components.InputFiles);
        }

        private static Task ChangeFilesAsync(HtmlRenderer renderer, CapturingComponentActivator components,
            params BrowserFile[] files) => renderer.Dispatcher.InvokeAsync(() =>
            components.InputFiles.Last().OnChange.InvokeAsync(new InputFileChangeEventArgs(files)));

        private static async Task AssertDownloadedBytesAsync(IStorage storage, StorageItem uploaded, byte[] expected)
        {
            var download = await storage.DownloadAsync(uploaded.FilePath);
            Assert.NotNull(download);
            await using (download)
            {
                await using var content = new MemoryStream();
                await download.Stream.CopyToAsync(content);
                Assert.Equal(expected, content.ToArray());
            }
        }

        private sealed class CapturingComponentActivator : IComponentActivator
        {
            private readonly List<IComponent> components = [];

            public List<InputFile> InputFiles { get; } = [];

            public IComponent CreateInstance(Type componentType)
            {
                var component = (IComponent)Activator.CreateInstance(componentType)!;
                components.Add(component);
                if (component is InputFile inputFile)
                {
                    InputFiles.Add(inputFile);
                }

                return component;
            }

            public T Single<T>() where T : IComponent => Assert.Single(components.OfType<T>());
        }

        private sealed class BrowserFile(string name, byte[] bytes) : IBrowserFile
        {
            public string Name => name;
            public DateTimeOffset LastModified => new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            public long Size => bytes.Length;
            public string ContentType => "text/plain";

            public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
            {
                if (Size > maxAllowedSize)
                {
                    throw new IOException("Browser file exceeds maximum size");
                }

                return new MemoryStream(bytes, writable: false);
            }
        }

        private sealed class FileUploadStorageOptions : StorageOptions, IFileSystemStorageOptions
        {
            public string StoragePath { get; set; } = "";
        }

        private sealed class StorageItemsInput : BaseStorageItemsInputComponent
        {
            public List<string> Errors { get; } = [];

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenComponent<InputFile>(0);
                builder.AddAttribute(1, nameof(InputFile.OnChange),
                    EventCallback.Factory.Create<InputFileChangeEventArgs>(this, UploadFilesAsync));
                builder.CloseComponent();
            }

            protected override Task NotifyMaxFilesCountExceededAsync(int filesCount)
            {
                Errors.Add($"Maximum of {MaxAllowedFiles} files is allowed. Selected: {filesCount}");
                return Task.CompletedTask;
            }
        }
    }
}
