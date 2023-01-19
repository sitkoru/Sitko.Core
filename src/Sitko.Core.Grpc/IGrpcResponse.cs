namespace Sitko.Core.Grpc;

[Obsolete("Do not implement this interface")]
public interface IGrpcResponse : IGrpcMessage
{
    ApiResponseInfo ResponseInfo { get; set; }
}
