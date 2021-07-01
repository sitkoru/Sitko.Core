using Serilog.Events;
using Sitko.Core.App.Localization;
using Sitko.Core.Apps.Blazor.Data;
using Sitko.Core.Blazor.AntDesignComponents;
using Sitko.Core.Db.Postgres;
using Sitko.Core.Repository.EntityFrameworkCore;
using Sitko.Core.Storage.FileSystem;

namespace Sitko.Core.Apps.Blazor
{
    public class TestBlazorApplication : AntBlazorApplication<Startup>
    {
        public TestBlazorApplication(string[] args) : base(args)
        {
            AddModule<PostgresModule<BarContext>, PostgresDatabaseModuleOptions<BarContext>>();
            AddModule<EFRepositoriesModule<BarContext>, EfRepositoriesModuleOptions>();
            AddModule<FileSystemStorageModule<TestBlazorStorageOptions>, TestBlazorStorageOptions>();
            ConfigureLogLevel("System.Net.Http.HttpClient.health-checks", LogEventLevel.Error)
                .ConfigureLogLevel("Microsoft.AspNetCore", LogEventLevel.Warning)
                .ConfigureLogLevel("Microsoft.EntityFrameworkCore", LogEventLevel.Warning);
            ConfigureServices((_, _, services) =>
            {
                services.AddJsonLocalization();
            });
        }
    }
}
