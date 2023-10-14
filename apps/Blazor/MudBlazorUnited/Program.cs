using FluentValidation;
using MudBlazorUnited.Components;
using MudBlazorUnited.Data;
using Sitko.Core.App.Localization;
using Sitko.Core.App.Web;
using Sitko.Core.Blazor.MudBlazor.Server;
using Sitko.Core.Blazor.MudBlazorComponents;
using Sitko.Core.Db.Postgres;
using Sitko.Core.Repository.EntityFrameworkCore;
using Sitko.Core.Storage;
using Sitko.Core.Storage.FileSystem;
using Sitko.Core.Storage.Metadata.Postgres;
using Sitko.Core.Storage.Remote;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddMudBlazorServer()
    .AddPostgresDatabase<BarContext>(options =>
    {
        options.EnableSensitiveLogging = true;
    })
    .AddEFRepositories<BarContext>()
    .AddFileSystemStorage<TestBlazorStorageOptions>()
    .AddRemoteStorage<TestRemoteStorageOptions>()
    .AddPostgresStorageMetadata<TestBlazorStorageOptions>()
    .AddJsonLocalization();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.Configure<MudLayoutOptions>(builder.Configuration.GetSection("MudLayout"));
builder.Services.AddControllers();
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.ConfigureLocalization("ru-RU");

app.MapControllers();

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
