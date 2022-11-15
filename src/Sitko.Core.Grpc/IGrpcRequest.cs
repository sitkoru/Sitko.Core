namespace Sitko.Core.Grpc;

public interface IGrpcRequest : IGrpcMessage
{
    ApiRequestInfo RequestInfo { get; set; }
}

