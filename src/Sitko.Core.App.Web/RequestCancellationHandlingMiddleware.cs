using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.App.Web;

internal sealed class RequestCancellationHandlingMiddleware(
    RequestDelegate next,
    ILogger<RequestCancellationHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogDebug("Request was canceled for {Method} {Path}", context.Request.Method, context.Request.Path);
        }
    }
}
