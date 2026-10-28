namespace Sitko.Core.Grpc.Server
{
    public class GrpcServerModule : BaseGrpcServerModule<GrpcServerModuleOptions>
    {
        private readonly Dictionary<string, Action<IGrpcServerModule>> registrations = [];
        public override string OptionsKey => "Grpc:Server";

        public void AddService<TService>() where TService : class =>
            registrations.TryAdd(GrpcServicesHelper.GetServiceName<TService>(), static module => module.RegisterService<TService>(null));

        protected override IReadOnlyDictionary<string, Action<IGrpcServerModule>> GetServiceRegistrations(GrpcServerModuleOptions options)
        {
            foreach (var (name, registration) in options.ServiceRegistrations)
            {
                _ = registrations.TryAdd(name, registration);
            }
            return registrations;
        }
    }
}
