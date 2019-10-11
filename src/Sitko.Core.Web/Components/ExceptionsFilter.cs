using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Sitko.Core.Web.Components
{
    public class ExceptionsFilter : ExceptionFilterAttribute
    {
        private readonly ILogger _logger;

        public ExceptionsFilter(ILogger<ExceptionsFilter> logger)
        {
            _logger = logger;
        }

        public override void OnException(ExceptionContext context)
        {
            base.OnException(context);
            using (LogContext.PushProperty(RequestIdMiddleware.RequestIdProperty, context.HttpContext.RequestId()))
            {
                _logger.LogError(500, context.Exception, context.Exception.Message);
            }
        }
    }
}
