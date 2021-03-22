namespace Sitko.Core.Grpc
{
    public interface IGrpcResponse : IGrpcMessage
    {
        ApiResponseInfo ResponseInfo { get; set; }
    }
}