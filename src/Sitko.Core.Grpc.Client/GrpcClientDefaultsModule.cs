using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.Configuration;
using Sitko.Core.App;

namespace Sitko.Core.Grpc.Client
{
    internal sealed class GrpcClientDefaultsModule : BaseApplicationModule
    {
        public override string OptionsKey => "Grpc:Client";
        internal List<Action<IApplicationContext, GrpcClientModuleOptions>> ConfigureActions { get; } = [];

        internal static void BindOptions(IConfigurationSection configuration, GrpcClientModuleOptions options)
        {
            var retryPolicy = configuration.GetSection(nameof(GrpcClientModuleOptions.RetryPolicy));
            if (retryPolicy.GetChildren().Any(static section =>
                    section.Key.Equals(nameof(RetryPolicy.RetryableStatusCodes), StringComparison.OrdinalIgnoreCase)))
            {
                options.RetryPolicy?.RetryableStatusCodes.Clear();
            }
            configuration.Bind(options);
        }

        internal void Apply(IApplicationContext context, GrpcClientModuleOptions options)
        {
            BindOptions(context.Configuration.GetSection("Grpc:Client:Default"), options);
            foreach (var configure in ConfigureActions)
            {
                configure(context, options);
            }
        }
    }
}
