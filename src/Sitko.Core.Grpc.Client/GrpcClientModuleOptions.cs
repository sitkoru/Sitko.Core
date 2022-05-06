using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Serialization;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using JetBrains.Annotations;
using Sitko.Core.App;

namespace Sitko.Core.Grpc.Client;

[PublicAPI]
public class GrpcClientModuleOptions<TClient> : BaseModuleOptions where TClient : ClientBase<TClient>
{
    public bool EnableHttp2UnencryptedSupport { get; set; }
    public bool DisableCertificatesValidation { get; set; }
    [JsonIgnore] public Action<GrpcChannelOptions>? ConfigureChannelOptions { get; set; }
    [JsonIgnore] public Func<HttpClientHandler, HttpMessageHandler>? ConfigureHttpHandler { get; set; }

    internal HashSet<Type> Interceptors { get; } = new();

    public GrpcClientModuleOptions<TClient> AddInterceptor<TInterceptor>() where TInterceptor : Interceptor
    {
        Interceptors.Add(typeof(TInterceptor));
        return this;
    }
}
