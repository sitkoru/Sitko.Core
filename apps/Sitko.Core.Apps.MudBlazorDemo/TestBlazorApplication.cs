using System;
using System.Net.Http;
using Sitko.Core.App.Localization;
using Sitko.Core.Apps.MudBlazorDemo.Data;
using Sitko.Core.Blazor.MudBlazor.Server;
using Sitko.Core.Db.Postgres;
using Sitko.Core.Repository.EntityFrameworkCore;
using Sitko.Core.Storage;
using Sitko.Core.Storage.FileSystem;
using Sitko.Core.Storage.Metadata.Postgres;
using Sitko.Core.Storage.Remote;

namespace Sitko.Core.Apps.MudBlazorDemo;

public class TestBlazorApplication : MudBlazorApplication<Startup>
{
    public TestBlazorApplication(string[] args) : base(args) =>
        this.AddPostgresDatabase<BarContext>(options =>
            {
                options.EnableSensitiveLogging = true;
            })
            .AddEFRepositories<BarContext>()
            // backend storage, in wasm it should be in server project
            .AddFileSystemStorage<TestBlazorStorageOptions>()
            // frontend storage, in wasm it should be in client project
            .AddRemoteStorage<TestRemoteStorageOptions>()
            .AddPostgresStorageMetadata<TestBlazorStorageOptions>()
            .AddJsonLocalization();
}

public class TestRemoteStorageOptions : StorageOptions, IRemoteStorageOptions
{
    public Uri RemoteUrl { get; set; } = new("https://localhost");
    public Func<HttpClient>? HttpClientFactory { get; set; }
}
