using Grpc.AspNetCore.Server;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Sitko.Core.App;
using Sitko.Core.App.Web;
using Sitko.Core.ServiceDiscovery;

namespace Sitko.Core.Grpc.Server
{
    public class GrpcServerModule : BaseApplicationModule<GrpcServerModuleOptions>, IGrpcServerModule,
        IHostBuilderModule<GrpcServerModuleOptions>, IWebApplicationModule<GrpcServerModuleOptions>
    {
        private readonly List<Action<IEndpointRouteBuilder>> endpointRegistrations = [];
        private bool enableGrpcWebEndpoints;
        private readonly List<Action<IServiceCollection, IApplicationContext>> serviceOptionsRegistrations = [];
        private readonly Dictionary<string, ServiceRegistration> registrations = [];
        internal List<Action<IApplicationContext, GrpcServerModuleOptions>> ConfigureActions { get; set; } = [];
        public override string OptionsKey => "Grpc:Server";

        public void AddService<TService>(Action<GrpcServiceOptions>? configure = null) where TService : class =>
            AddService<TService>(false, configure);

        public void AddServiceWithGrpcWeb<TService>(Action<GrpcServiceOptions>? configure = null) where TService : class =>
            AddService<TService>(true, configure);

        private void AddService<TService>(bool enableGrpcWeb, Action<GrpcServiceOptions>? configure) where TService : class
        {
            var name = GrpcServicesHelper.GetServiceName<TService>();
            var registration = new ServiceRegistration(typeof(TService),
                static (module, options, web, specific) =>
                    module.RegisterService<TService>(options.RequiredAuthorizationSchemeName, web, specific),
                enableGrpcWeb, configure);
            if (registrations.TryGetValue(name, out var existing))
            {
                if (existing.ServiceType != registration.ServiceType)
                {
                    throw new InvalidOperationException($"Contract {name} has conflicting implementations: {existing.ServiceType} and {registration.ServiceType}");
                }
                registration = existing with
                {
                    EnableGrpcWeb = existing.EnableGrpcWeb || registration.EnableGrpcWeb,
                    Configure = existing.Configure + registration.Configure
                };
            }
            registrations[name] = registration;
        }

        internal static void ConfigureOptions(IApplicationContext context, GrpcServerModuleOptions options,
            List<Action<IApplicationContext, GrpcServerModuleOptions>> configureActions)
        {
            var configureService = options.ConfigureGrpcService;
            foreach (var configure in configureActions)
            {
                options.ConfigureGrpcService = null;
                configure(context, options);
                configureService += options.ConfigureGrpcService;
            }
            options.ConfigureGrpcService = configureService;
        }

        public virtual void RegisterService<TService>(string? requiredAuthorizationSchemeName, bool enableGrpcWeb = false,
            Action<GrpcServiceOptions>? configure = null)
            where TService : class
        {
            enableGrpcWebEndpoints |= enableGrpcWeb;
            serviceOptionsRegistrations.Add((services, context) =>
                _ = services.AddGrpc().AddServiceOptions<TService>(serviceOptions =>
                {
                    serviceOptions.CompressionProviders = [.. serviceOptions.CompressionProviders];
                    context.Configuration.GetSection($"{OptionsKey}:Contracts:{GrpcServicesHelper.GetServiceName<TService>()}")
                        .Bind(serviceOptions);
                    configure?.Invoke(serviceOptions);
                }));
            endpointRegistrations.Add(builder =>
            {
                var grpcEndpoint = builder.MapGrpcService<TService>();
                if (enableGrpcWeb)
                {
                    grpcEndpoint = grpcEndpoint.EnableGrpcWeb();
                }

                if (!string.IsNullOrEmpty(requiredAuthorizationSchemeName))
                {
                    grpcEndpoint.RequireAuthorization(new AuthorizeAttribute
                    {
                        AuthenticationSchemes = requiredAuthorizationSchemeName,
                    });
                }
            });
        }

        public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
            GrpcServerModuleOptions startupOptions)
        {
            base.ConfigureServices(applicationContext, services, startupOptions);
            services.AddScoped(typeof(IGrpcCallProcessor<>), typeof(GrpcCallProcessor<>));
            _ = services.AddGrpc(options => ConfigureServiceOptions(applicationContext, startupOptions, options));
            if (startupOptions.EnableReflection)
            {
                services.AddGrpcReflection();
            }

            foreach (var (name, registration) in registrations)
            {
                registration.Register(this, startupOptions, registration.EnableGrpcWeb, registration.Configure);
                _ = services.AddToServiceDiscovery(new ServiceDiscoveryService(GrpcModuleConstants.GrpcServiceDiscoveryType,
                    name, new Dictionary<string, string>(), startupOptions.ServiceDiscoveryPortNames));
            }
            foreach (var configureServiceOptions in serviceOptionsRegistrations)
            {
                configureServiceOptions(services, applicationContext);
            }
        }

        private void ConfigureServiceOptions(IApplicationContext context, GrpcServerModuleOptions startupOptions,
            GrpcServiceOptions options)
        {
            context.Configuration.GetSection($"{OptionsKey}:Default").Bind(options);
            if (startupOptions.EnableDetailedErrors is { } enableDetailedErrors)
            {
                options.EnableDetailedErrors = enableDetailedErrors;
            }
            startupOptions.ConfigureGrpcService?.Invoke(options);
        }

        public void ConfigureWebHost(IApplicationContext applicationContext, ConfigureWebHostBuilder webHostBuilder,
            GrpcServerModuleOptions options) =>
            options.ConfigureWebHostDefaults?.Invoke(webHostBuilder);

        public void ConfigureEndpoints(IApplicationContext applicationContext,
            IApplicationBuilder appBuilder, IEndpointRouteBuilder endpoints)
        {
            var config = GetOptions(appBuilder.ApplicationServices);
            if (enableGrpcWebEndpoints)
            {
                appBuilder.UseGrpcWeb();
            }

            foreach (var endpointRegistration in endpointRegistrations)
            {
                endpointRegistration(endpoints);
            }

            endpoints.MapGrpcService<HealthService>();
            if (config.EnableReflection)
            {
                endpoints.MapGrpcReflectionService();
            }
        }

        private sealed record ServiceRegistration(Type ServiceType,
            Action<IGrpcServerModule, GrpcServerModuleOptions, bool, Action<GrpcServiceOptions>?> Register,
            bool EnableGrpcWeb, Action<GrpcServiceOptions>? Configure);
    }
}
