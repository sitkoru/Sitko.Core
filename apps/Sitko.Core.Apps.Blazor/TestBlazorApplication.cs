using System;
using System.IO;
using Serilog.Events;
using Sitko.Core.App.Blazor;
using Sitko.Core.Apps.Blazor.Data;
using Sitko.Core.Apps.Blazor.Pages;
using Sitko.Core.Blazor.AntDesignComponents;
using Sitko.Core.Db.InMemory;
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
                (configuration, environment, moduleConfig) =>
                {
                    moduleConfig.PublicUri = new Uri("http://localhost");
                    moduleConfig.Name = "Test";
                    moduleConfig.StoragePath = Path.Combine(Path.GetTempPath(), "test-blazor-upload");
                });
            ConfigureLogLevel("System.Net.Http.HttpClient.health-checks",
                LogEventLevel.Error).ConfigureLogLevel("Microsoft.AspNetCore.Components", LogEventLevel.Warning);
            ConfigureLogLevel("Microsoft.AspNetCore.SignalR", LogEventLevel.Warning);
            ConfigureLogLevel("Microsoft.EntityFrameworkCore.ChangeTracking", LogEventLevel.Warning);
        }
    }
}
