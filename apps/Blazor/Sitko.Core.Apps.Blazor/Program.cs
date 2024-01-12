using FluentValidation;
using Sitko.Core.App.Localization;
using Sitko.Core.App.Web;
using Sitko.Core.Apps.Blazor;
using Sitko.Core.Apps.Blazor.Client;
using Sitko.Core.Apps.Blazor.Components;
using Sitko.Core.Apps.Blazor.Data;
using Sitko.Core.Blazor.MudBlazor.Server;
using Sitko.Core.Blazor.MudBlazorComponents;
using Sitko.Core.Blazor.Server;
using Sitko.Core.Db.Postgres;
using Sitko.Core.Repository.EntityFrameworkCore;
using Sitko.Core.Storage.FileSystem;
using Sitko.Core.Storage.Metadata.Postgres;
using Sitko.Core.Storage.Remote;
using Index = Sitko.Core.Apps.Blazor.Client.Pages.Index;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder
    .AddSitkoCoreBlazorServer()
    .AddMudBlazorServer()
    .AddInteractiveWebAssembly()
    .AddPostgresDatabase<BarContext>(options =>
    {
        options.EnableSensitiveLogging = true;
    })
    .AddEFRepositories<BarContext>()

    // frontend storage, in wasm it should be in client project
    .AddRemoteStorage<TestRemoteStorageOptions>()
    .AddPostgresStorageMetadata<TestBlazorStorageOptions>()
    .AddJsonLocalization(options =>
    {
        options.AddDefaultResource<App>();
    });

// backend storage, in wasm it should be in server project
builder.AddFileSystemStorage<TestBlazorStorageOptions>();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.Configure<MudLayoutOptions>(builder.Configuration.GetSection("MudLayout"));
builder.Services.AddControllers();
var app = builder.Build();

app.ConfigureLocalization("ru-RU");

app.MapSitkoCoreBlazor<App>(true)
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Index).Assembly);

app.Run();
