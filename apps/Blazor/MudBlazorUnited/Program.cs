using FluentValidation;
using MudBlazorUnited.Components;
using MudBlazorUnited.Data;
using MudBlazorUnited.Tasks;
using MudBlazorUnited.Tasks.Demo;
using Sitko.Core.App.Localization;
using Sitko.Core.App.Web;
using Sitko.Core.Auth.IdentityServer;
using Sitko.Core.Blazor.MudBlazor.Server;
using Sitko.Core.Blazor.MudBlazorComponents;
using Sitko.Core.Db.Postgres;
using Sitko.Core.Repository.EntityFrameworkCore;
using Sitko.Core.Storage;
using Sitko.Core.Storage.FileSystem;
using Sitko.Core.Storage.Metadata.Postgres;
using Sitko.Core.Storage.Remote;
using Sitko.Core.Tasks.Kafka;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddMudBlazorServer()
    .ConfigureWeb(options =>
    {
        options.EnableMvc = true;
        options.EnableStaticFiles = true;
    })
    .AddOidcIdentityServer(options =>
    {
        options.AddPolicy("AuthenticatedUser", policyBuilder => policyBuilder.RequireAuthenticatedUser(), true);
    })
    .AddEFRepositories<BarContext>()
    .AddRemoteStorage<TestRemoteStorageOptions>()
    .AddPostgresStorageMetadata<TestBlazorStorageOptions>()
    .AddJsonLocalization();

builder.AddFileSystemStorage<TestBlazorStorageOptions>()
    .AddPostgresDatabase<BarContext>(options =>
    {
        options.EnableSensitiveLogging = true;
    });

builder.AddKafkaTasks<MudBlazorBaseTask, MudBlazorTasksDbContext>(options =>
    {
        options
            .AddTask<LoggingTask, LoggingTaskConfig, LoggingTaskResult>("* * * * *")
            .AddExecutorsFromAssemblyOf<MudBlazorBaseTask>();
    }, true,
    options => options.AutoApplyMigrations = true);

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.Configure<MudLayoutOptions>(builder.Configuration.GetSection("MudLayout"));
builder.Services.AddControllers();
var app = builder.Build();

app.ConfigureLocalization("ru-RU");

app.MapSitkoCore();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public class TestRemoteStorageOptions : StorageOptions, IRemoteStorageOptions
{
    public Uri RemoteUrl { get; set; } = new("https://localhost");
    public Func<HttpClient>? HttpClientFactory { get; set; }
}

public class TestBlazorStorageOptions : StorageOptions, IFileSystemStorageOptions
{
    public string StoragePath { get; set; } = "";
}
