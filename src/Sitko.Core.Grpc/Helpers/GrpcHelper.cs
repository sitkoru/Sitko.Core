using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Grpc.Helpers
{
    public static class GrpcHelper
    {
        public static string PrepareString(string s)
        {
            return s ?? string.Empty;
        }

        public static void SetError(this ApiResponseInfo responseInfo, string error, int code = 500)
        {
            responseInfo.IsSuccess = false;
            responseInfo.Error = new ApiResponseError {Code = code, Errors = {error}};
        }

        public static void SetErrors(this ApiResponseInfo responseInfo, IEnumerable<string> errors, int code = 500)
        {
            responseInfo.IsSuccess = false;
            responseInfo.Error = new ApiResponseError {Code = code, Errors = {errors}};
        }

        public static void SetException(this ApiResponseInfo responseInfo, Exception ex, int code = 500)
        {
            responseInfo.IsSuccess = false;
            responseInfo.Error = new ApiResponseError {Code = code, Errors = {ex.ToString()}};
        }

        public static void SetException(this ApiResponseInfo responseInfo, Exception ex, ILogger logger, object request,
            int code = 500, [CallerMemberName] string? methodName = null)
        {
            responseInfo.SetException(ex, code);
            logger.LogError(ex, "Error in method {MethodName}. Request: {@Request}. Error: {ErrorText}", methodName,
                request, ex.ToString());
        }
    }
}
