using JetBrains.Annotations;

namespace Sitko.Core.Grpc.Extensions;

[PublicAPI]
public static class GrpcExtensions
{
    public static string PrepareString(string? s) => s ?? string.Empty;

    public static void SetError(this IGrpcResponse response, string error, int code = 500)
    {
        response.ResponseInfo.IsSuccess = false;
        response.ResponseInfo.Error = new ApiResponseError { Code = code, Errors = { error } };
    }

    public static void SetErrors(this IGrpcResponse response, IEnumerable<string> errors, int code = 500)
    {
        response.ResponseInfo.IsSuccess = false;
        response.ResponseInfo.Error = new ApiResponseError { Code = code, Errors = { errors } };
    }

    public static void SetException(this IGrpcResponse response, Exception ex, int code = 500)
    {
        response.ResponseInfo.IsSuccess = false;
        response.ResponseInfo.Error = new ApiResponseError { Code = code, Errors = { ex.ToString() } };
    }

    public static bool IsSuccess(this IGrpcResponse response) => response.ResponseInfo.IsSuccess;
}

