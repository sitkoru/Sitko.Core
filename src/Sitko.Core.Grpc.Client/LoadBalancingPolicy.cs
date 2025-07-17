namespace Sitko.Core.Grpc.Client;

public enum LoadBalancingPolicy
{
    None = 0,
    RoundRobin = 1,
    PickFirst = 2
}
