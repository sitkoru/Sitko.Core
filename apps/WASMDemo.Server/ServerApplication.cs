using FluentValidation;
using MudBlazor.Services;
using Sitko.Core.App.Web;
using Sitko.Core.Blazor;
using Sitko.Core.Db.Postgres;
using Sitko.Core.Repository.EntityFrameworkCore;
using WASMDemo.Shared;

namespace WASMDemo.Server;

public class ServerApplication : WebApplication<Startup>
{
    public ServerApplication(string[] args) : base(args)
    {
        this.AddPersistentState()
            .AddPostgresDatabase<WasmDbContext>(options =>
            {
                options.EnableContextPooling = false;
                options.DbContextFactoryLifetime = ServiceLifetime.Scoped;
            })
            .AddEFRepositories<WasmDbContext>();
    }
}


public class Startup : BaseStartup
{
    public Startup(IConfiguration configuration, IHostEnvironment environment) : base(configuration, environment)
    {
    }

    protected override void ConfigureAppServices(IServiceCollection services)
    {
        base.ConfigureAppServices(services);
        services.AddValidatorsFromAssemblyContaining<Startup>();
        services.AddControllersWithViews();
        services.AddRazorPages();
        services.AddScoped(sp =>
        {
            var uri = new Uri("https://localhost:7299");
            if (bool.TryParse(System.Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), out var inContainer) &&
                inContainer)
            {
                uri = new Uri("http://localhost");
            }

            return new HttpClient
            {
                BaseAddress = uri
            };
        });
        services.AddResponseCompression();
        services.AddMudServices();
    }

    protected override bool EnableStaticFiles => false;

    protected override void ConfigureBeforeRoutingMiddleware(IApplicationBuilder app)
    {
        base.ConfigureBeforeRoutingMiddleware(app);
        app.ConfigureLocalization("ru-RU");
        if (Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();
    }


    protected override void ConfigureEndpoints(IApplicationBuilder app, IEndpointRouteBuilder endpoints)
    {
        base.ConfigureEndpoints(app, endpoints);
        endpoints.MapControllers();
        endpoints.MapFallbackToPage("/_Host");
    }
}
