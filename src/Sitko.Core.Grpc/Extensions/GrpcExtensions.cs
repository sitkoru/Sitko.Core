using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Grpc.Extensions
{
    public static class GrpcExtensions
    {
        public static string PrepareString(string? s)
        {
            return s ?? string.Empty;
        }

        public static void SetError(this IGrpcResponse response, string error, int code = 500)
        {
            response.ResponseInfo.IsSuccess = false;
            response.ResponseInfo.Error = new ApiResponseError {Code = code, Errors = {error}};
        }

        public static void SetErrors(this IGrpcResponse response, IEnumerable<string> errors, int code = 500)
        {
            response.ResponseInfo.IsSuccess = false;
            response.ResponseInfo.Error = new ApiResponseError {Code = code, Errors = {errors}};
        }

        public static void SetException(this IGrpcResponse response, Exception ex, int code = 500)
        {
            response.ResponseInfo.IsSuccess = false;
            response.ResponseInfo.Error = new ApiResponseError {Code = code, Errors = {ex.ToString()}};
        }

        public static void SetException<TRequest>(this IGrpcResponse response, Exception ex, ILogger? logger,
            TRequest request,
            int code = 500, string? methodName = null) where TRequest : IGrpcRequest
        {
            response.SetException(ex, code);
            logger?.LogError(ex, "Error in method {MethodName}. Request: {@Request}. Error: {ErrorText}", methodName,
                request, ex.ToString());
        }
    }
}
