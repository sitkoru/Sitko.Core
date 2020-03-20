using Sitko.Core.App;

namespace Sitko.Core.Grpc.Client
{
    public abstract class GrpcClientModule : BaseApplicationModule<GrpcClientModuleConfig>
    {
        public GrpcClientModule(GrpcClientModuleConfig config, Application application) : base(config, application)
        {
        }
    }

    public class GrpcClientModuleConfig
    {
        public bool EnableHttp2UnencryptedSupport { get; set; }
        public bool DisableCertificatesValidation { get; set; }
    }
}
