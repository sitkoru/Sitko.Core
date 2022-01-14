using Sitko.Core.App.Localization;
using Sitko.Core.Apps.MudBlazorDemo.Data;
using Sitko.Core.Blazor.MudBlazor.Server;
using Sitko.Core.Db.Postgres;
using Sitko.Core.Repository.EntityFrameworkCore;
using Sitko.Core.Storage.FileSystem;
using Sitko.Core.Storage.Metadata.Postgres;

namespace Sitko.Core.Apps.MudBlazorDemo;

public class TestBlazorApplication : MudBlazorApplication<Startup>
{
    public TestBlazorApplication(string[] args) : base(args) =>
        this.AddPostgresDatabase<BarContext>(options =>
            {
                options.EnableSensitiveLogging = true;
            })
            .AddEFRepositories<BarContext>()
            .AddFileSystemStorage<TestBlazorStorageOptions>()
            .AddPostgresStorageMetadata<TestBlazorStorageOptions>()
            .AddJsonLocalization();
}
