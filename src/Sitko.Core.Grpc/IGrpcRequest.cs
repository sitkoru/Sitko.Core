namespace Sitko.Core.Grpc;

[Obsolete("Do not implement this interface")]
public interface IGrpcRequest : IGrpcMessage
{
    ApiRequestInfo RequestInfo { get; set; }
}
