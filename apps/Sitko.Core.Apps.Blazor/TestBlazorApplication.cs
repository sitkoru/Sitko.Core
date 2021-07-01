using System;
using System.IO;
using Serilog.Events;
using Sitko.Core.App;
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
            AddModule<PostgresModule<BarContext>, PostgresDatabaseModuleConfig<BarContext>>();
            AddModule<EFRepositoriesModule<BarContext>, EFRepositoriesModuleConfig>();
            AddModule<FileSystemStorageModule<TestBlazorStorageOptions>, TestBlazorStorageOptions>(
                (_, _, moduleConfig) =>
                {
                    moduleConfig.PublicUri = new Uri("https://localhost:5001/static/");
                    moduleConfig.Name = "Test";
                    moduleConfig.StoragePath = Path.Combine(Path.GetFullPath("wwwroot"), "static");
                });
            ConfigureLogLevel("System.Net.Http.HttpClient.health-checks",
                LogEventLevel.Error).ConfigureLogLevel("Microsoft.AspNetCore.Components", LogEventLevel.Warning);
            ConfigureLogLevel("Microsoft.AspNetCore.SignalR", LogEventLevel.Warning);
            ConfigureLogLevel("Microsoft.EntityFrameworkCore.ChangeTracking", LogEventLevel.Warning);
            this.ConfigureServices(collection =>
            {
                collection.AddJsonLocalization();
            });
        }
    }
}
