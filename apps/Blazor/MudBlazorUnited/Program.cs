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

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder
    .AddMudBlazorServer()
    .AddPostgresDatabase<BarContext>(options =>
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

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.Configure<MudLayoutOptions>(builder.Configuration.GetSection("MudLayout"));
builder.Services.AddControllers();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
