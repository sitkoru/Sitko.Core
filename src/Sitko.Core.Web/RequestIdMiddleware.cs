using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Sitko.Core.Web
{
    [SuppressMessage("ReSharper", "UseAsyncSuffix")]
    public class RequestIdMiddleware
    {
        public const string RequestIdProperty = "NgRequestId";

        private readonly RequestDelegate _next;

        public RequestIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            string requestId;
            if (context.Request.Headers.ContainsKey("X-Request-ID"))
            {
                requestId = context.Request.Headers["X-Request-ID"][0];
            }
            else
            {
                requestId = Guid.NewGuid().ToString();
            }

            context.Features.Set<IRequestIdFeature>(new RequestIdFeature(requestId));
            using (LogContext.PushProperty(RequestIdProperty, requestId))
            {
                await _next.Invoke(context);
            }
        }
    }

    public interface IRequestIdFeature
    {
        string RequestId { get; }
    }

    public class RequestIdFeature : IRequestIdFeature
    {
        public string RequestId { get; }

        public RequestIdFeature(string requestId)
        {
            RequestId = requestId;
        }
    }

    public static class HttpContextExtension
    {
        public static string RequestId(this HttpContext context)
        {
            var requestIdFeature = context.Features.Get<IRequestIdFeature>();
            return requestIdFeature?.RequestId;
        }
    }
}
