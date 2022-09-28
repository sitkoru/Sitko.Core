using System;
using Grpc.Core;

namespace Sitko.Core.Grpc.Client;

/// <summary>
///     Context used to update <see cref="Grpc.Core.CallOptions" /> for a gRPC call.
/// </summary>
public sealed class CallOptionsContext
{
    internal CallOptionsContext(CallOptions callOptions, IServiceProvider serviceProvider)
    {
        CallOptions = callOptions;
        ServiceProvider = serviceProvider;
    }

    /// <summary>
    ///     Gets or sets the call options.
    /// </summary>
    public CallOptions CallOptions { get; set; }

    /// <summary>
    ///     Gets the service provider.
    /// </summary>
    public IServiceProvider ServiceProvider { get; }
}
