using Microsoft.Extensions.Configuration;
using Sitko.Core.App;

namespace Sitko.Core.Grpc.Client
{
    internal sealed class GrpcClientDefaultsModule : BaseApplicationModule
    {
        public override string OptionsKey => "Grpc:Client";
        internal List<Action<IApplicationContext, GrpcClientModuleOptions>> ConfigureActions { get; } = [];

        internal void Apply(IApplicationContext context, GrpcClientModuleOptions options)
        {
            context.Configuration.GetSection("Grpc:Client:Default").Bind(options);
            foreach (var configure in ConfigureActions)
            {
                configure(context, options);
            }
        }
    }
}
