using FluentValidation;
using Sitko.Core.App.Localization;
using Sitko.Core.App.Web;
using Sitko.Core.Apps.Blazor;
using Sitko.Core.Apps.Blazor.Components;
using Sitko.Core.Apps.Blazor.Data;
using Sitko.Core.Blazor.MudBlazor.Server;
using Sitko.Core.Blazor.MudBlazorComponents;
using Sitko.Core.Blazor.Server;
using Sitko.Core.Storage.FileSystem;
using Index = Sitko.Core.Apps.Blazor.Client.Pages.Index;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder
    .AddSitkoCoreBlazorServer()
    .AddMudBlazorServer()
    .AddInteractiveWebAssembly()
    .AddJsonLocalization(static options => options.AddDefaultResource<App>());

builder.AddFileSystemStorage<TestBlazorStorageOptions>();
builder.AddFileSystemStorageMetadata<TestBlazorStorageOptions>();

builder.Services.AddValidatorsFromAssemblyContaining<DemoFormModelValidator>();
builder.Services.Configure<MudLayoutOptions>(builder.Configuration.GetSection("MudLayout"));
builder.Services.AddControllers();
var app = builder.Build();

app.ConfigureLocalization("ru-RU");
app.MapStaticAssets();
app.MapSitkoCoreBlazor<App>(true)
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Index).Assembly);

app.Run();
