using System;
using System.Collections.Generic;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Sitko.Core.App;

namespace Sitko.Core.Grpc.Client
{
    using System.Text.Json.Serialization;

    public class GrpcClientModuleOptions : BaseModuleOptions
    {
        public bool EnableHttp2UnencryptedSupport { get; set; }
        public bool DisableCertificatesValidation { get; set; }
        [JsonIgnore] public Action<GrpcChannelOptions>? ConfigureChannelOptions { get; set; }

        internal HashSet<Type> Interceptors { get; } = new();

        public GrpcClientModuleOptions AddInterceptor<TInterceptor>() where TInterceptor : Interceptor
        {
            Interceptors.Add(typeof(TInterceptor));
            return this;
        }
    }
}
