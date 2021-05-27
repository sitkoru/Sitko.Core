using System;
using System.Collections.Generic;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Sitko.Core.App;

namespace Sitko.Core.Grpc.Client
{
    public class GrpcClientModuleConfig : BaseModuleConfig
    {
        public bool EnableHttp2UnencryptedSupport { get; set; }
        public bool DisableCertificatesValidation { get; set; }
        public Action<GrpcChannelOptions>? ConfigureChannelOptions { get; set; }

        internal readonly HashSet<Type> Interceptors = new HashSet<Type>();

        public GrpcClientModuleConfig AddInterceptor<TInterceptor>() where TInterceptor : Interceptor
        {
            Interceptors.Add(typeof(TInterceptor));
            return this;
        }
    }
}
