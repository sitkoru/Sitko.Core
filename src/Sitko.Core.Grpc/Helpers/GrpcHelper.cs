using System;
using System.Collections.Generic;
using Google.Protobuf.WellKnownTypes;

namespace Sitko.Core.Grpc.Helpers
{
    public static class GrpcHelper
    {
        public static ApiRequestInfo GetRequestInfo(int? projectId = null, int? userId = null, bool userIsAdmin = false,
            IEnumerable<string> userFlags = null)
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
    }
}
