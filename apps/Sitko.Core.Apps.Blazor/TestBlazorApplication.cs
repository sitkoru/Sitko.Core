using Microsoft.Extensions.Hosting;
using Sitko.Core.App.Localization;
using Sitko.Core.Apps.Blazor.Data;
using Sitko.Core.Blazor.AntDesignComponents;
using Sitko.Core.Db.Postgres;
using Sitko.Core.Repository.EntityFrameworkCore;
using Sitko.Core.Storage.FileSystem;
using Sitko.Core.Storage.Metadata.Postgres;

namespace Sitko.Core.Apps.Blazor
{
    public class TestBlazorApplication : AntBlazorApplication<Startup>
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

        protected override bool LoggingEnableConsole(HostBuilderContext context) => true;
    }
}
