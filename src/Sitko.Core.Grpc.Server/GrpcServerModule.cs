using Grpc.AspNetCore.Server;
using Sitko.Core.App;

namespace Sitko.Core.Grpc.Server
{
    public class GrpcServerModule : BaseGrpcServerModule<GrpcServerModuleOptions>
    {
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

        protected override IReadOnlyDictionary<string, Action<IGrpcServerModule>> GetServiceRegistrations(GrpcServerModuleOptions options) =>
            registrations.ToDictionary(static pair => pair.Key, pair => new Action<IGrpcServerModule>(module =>
                pair.Value.Register(module, options, pair.Value.EnableGrpcWeb, pair.Value.Configure)));

        private sealed record ServiceRegistration(Type ServiceType,
            Action<IGrpcServerModule, GrpcServerModuleOptions, bool, Action<GrpcServiceOptions>?> Register,
            bool EnableGrpcWeb, Action<GrpcServiceOptions>? Configure);
    }
}
