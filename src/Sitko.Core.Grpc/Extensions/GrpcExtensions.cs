using JetBrains.Annotations;
using Sitko.Core.App.Results;

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

    public static OperationResult GetResult(this ApiResponseInfo apiResponseInfo) => apiResponseInfo.IsSuccess
        ? new OperationResult()
        : new OperationResult(apiResponseInfo.Error.ErrorsString);

    public static OperationResult<T> GetResult<T>(this ApiResponseInfo apiResponseInfo, T result) => apiResponseInfo.IsSuccess
        ? new OperationResult<T>(result)
        : new OperationResult<T>(apiResponseInfo.Error.ErrorsString);

    public static OperationResult<T> GetResult<T>(this ApiResponseInfo apiResponseInfo, Func<T> func) => !apiResponseInfo.IsSuccess
        ? new OperationResult<T>(apiResponseInfo.Error.ErrorsString)
        : new OperationResult<T>(func());
}
