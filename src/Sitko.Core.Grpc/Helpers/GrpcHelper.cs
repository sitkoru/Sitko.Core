using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Grpc.Helpers
{
    public static class GrpcHelper
    {
        public static ApiRequestInfo GetRequestInfo(int? projectId = null, int? userId = null, bool userIsAdmin = false,
            IEnumerable<string>? userFlags = null)
        {
            var requestInfo = new ApiRequestInfo
            {
                Id = Guid.NewGuid().ToString(),
                Date = DateTimeOffset.UtcNow.ToTimestamp(),
                UserIsAdmin = userIsAdmin
            };
            if (projectId.HasValue)
            {
                requestInfo.ProjectId = projectId.Value;
            }

            if (userId.HasValue)
            {
                requestInfo.UserId = userId.Value;
            }

            if (userFlags != null)
            {
                requestInfo.UserFlags.AddRange(userFlags);
            }

            return requestInfo;
        }

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
