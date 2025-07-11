using Grpc.Core;
using Grpc.Net.Client.Configuration;

namespace Sitko.Core.Grpc.Client;

public static class GrpcClientRetryPolicies
{
    public static RetryPolicy Default { get; } = new()
    {
        MaxAttempts = 5,
        InitialBackoff = TimeSpan.FromSeconds(1),
        MaxBackoff = TimeSpan.FromSeconds(5),
        BackoffMultiplier = 1.5,
        RetryableStatusCodes =
        {
            StatusCode.Aborted,
            StatusCode.DeadlineExceeded,
            StatusCode.ResourceExhausted,
            StatusCode.Unavailable
        }
    };
}
