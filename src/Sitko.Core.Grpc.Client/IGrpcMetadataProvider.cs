namespace Sitko.Core.Grpc.Client;

public interface IGrpcMetadataProvider
{
    Task<Dictionary<string, string>?> GetMetadataAsync();
}
