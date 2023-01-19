using JetBrains.Annotations;

namespace Sitko.Core.Grpc.Extensions;

[PublicAPI]
public static class GrpcExtensions
{
    public static string PrepareString(string? s) => s ?? string.Empty;

    [Obsolete("Do not use this method in new code")]
    public static void SetError(this IGrpcResponse response, string error, int code = 500)
    {
        response.ResponseInfo.IsSuccess = false;
        response.ResponseInfo.Error = new ApiResponseError { Code = code, Errors = { error } };
    }

    [Obsolete("Do not use this method in new code")]
    public static void SetErrors(this IGrpcResponse response, IEnumerable<string> errors, int code = 500)
    {
        response.ResponseInfo.IsSuccess = false;
        response.ResponseInfo.Error = new ApiResponseError { Code = code, Errors = { errors } };
    }

    [Obsolete("Do not use this method in new code")]
    public static void SetException(this IGrpcResponse response, Exception ex, int code = 500)
    {
        response.ResponseInfo.IsSuccess = false;
        response.ResponseInfo.Error = new ApiResponseError { Code = code, Errors = { ex.ToString() } };
    }

    [Obsolete("Do not use this method in new code")]
    public static bool IsSuccess(this IGrpcResponse response) => response.ResponseInfo.IsSuccess;
}
