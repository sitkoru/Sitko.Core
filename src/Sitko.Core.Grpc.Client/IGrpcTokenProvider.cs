namespace Sitko.Core.Grpc.Client;

public interface IGrpcTokenProvider
{
    Task<string> GetTokenAsync();
}
