using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Sitko.Core.App.Web.Razor
{
    public class ViewToStringRendererService<TConfig> : ViewExecutor where TConfig : IViewToStringRendererServiceOptions
    {
        private readonly IOptionsMonitor<TConfig> options;
        private readonly IActionContextAccessor actionContextAccessor;
        private readonly ITempDataProvider tempDataProvider;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IServiceProvider serviceProvider;

        public ViewToStringRendererService(
            IOptionsMonitor<TConfig> options,
            IActionContextAccessor actionContextAccessor,
            IOptions<MvcViewOptions> viewOptions,
            IHttpResponseStreamWriterFactory writerFactory,
            ICompositeViewEngine viewEngine,
            ITempDataDictionaryFactory tempDataFactory,
            DiagnosticListener diagnosticListener,
            IModelMetadataProvider modelMetadataProvider,
            ITempDataProvider tempDataProvider,
            IHttpContextAccessor httpContextAccessor,
            IServiceProvider serviceProvider)
            : base(viewOptions, writerFactory, viewEngine, tempDataFactory, diagnosticListener, modelMetadataProvider)
        {
            this.options = options;
            this.actionContextAccessor = actionContextAccessor;
            this.tempDataProvider = tempDataProvider;
            this.httpContextAccessor = httpContextAccessor;
            this.serviceProvider = serviceProvider;
        }

        public async Task<string> RenderViewToStringAsync<TModel>(string viewName, TModel model)
        {
            var context = GetActionContext();

            if (context == null)
            {
                throw new InvalidOperationException("Action context is null");
            }

            var result = new ViewResult()
            {
                ViewData = new ViewDataDictionary(
                    new EmptyModelMetadataProvider(),
                    new ModelStateDictionary()) {Model = model,},
                TempData = new TempDataDictionary(
                    context.HttpContext,
                    tempDataProvider),
                ViewName = viewName,
            };

            var viewEngineResult = FindView(context, result);
            viewEngineResult.EnsureSuccessful(null);

            var view = viewEngineResult.View;

            using (var output = new StringWriter())
            {
                var viewContext = new ViewContext(
                    context,
                    view,
                    new ViewDataDictionary(
                        new EmptyModelMetadataProvider(),
                        new ModelStateDictionary()) {Model = model},
                    new TempDataDictionary(
                        context.HttpContext,
                        tempDataProvider),
                    output,
                    new HtmlHelperOptions());

                await view.RenderAsync(viewContext);

                return output.ToString();
            }
        }

        private ActionContext GetActionContext()
        {
            var context = actionContextAccessor.ActionContext;
            if (context == null)
            {
                var httpContext = httpContextAccessor.HttpContext;
                if (httpContext == null)
                {
                    httpContext = new DefaultHttpContext
                    {
                        RequestServices = serviceProvider.CreateScope().ServiceProvider,
                        Request =
                        {
                            Protocol = "GET",
                            Host = new HostString(options.CurrentValue.Host),
                            Scheme = options.CurrentValue.Scheme
                        }
                    };
                }

                context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            }

            return context;
        }

        private ViewEngineResult FindView(ActionContext actionContext, ViewResult viewResult)
        {
            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            if (viewResult == null)
            {
                throw new ArgumentNullException(nameof(viewResult));
            }

            var viewEngine = viewResult.ViewEngine ?? ViewEngine;

            var viewName = viewResult.ViewName ?? GetActionName(actionContext);

            var result = viewEngine.GetView(null, viewName, true);
            var originalResult = result;
            if (!result.Success)
            {
                result = viewEngine.FindView(actionContext, viewName, true);
            }

            if (!result.Success)
            {
                if (originalResult.SearchedLocations.Any())
                {
                    if (result.SearchedLocations.Any())
                    {
                        // Return a new ViewEngineResult listing all searched locations.
                        var locations = new List<string>(originalResult.SearchedLocations);
                        locations.AddRange(result.SearchedLocations);
                        result = ViewEngineResult.NotFound(viewName, locations);
                    }
                    else
                    {
                        // GetView() searched locations but FindView() did not. Use first ViewEngineResult.
                        result = originalResult;
                    }
                }
            }

            if (!result.Success)
            {
                throw new InvalidOperationException($"Couldn't find view '{viewName}'");
            }

            return result;
        }


        private const string ActionNameKey = "action";

        private static string? GetActionName(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!context.RouteData.Values.TryGetValue(ActionNameKey, out var routeValue))
            {
                return null;
            }

            var actionDescriptor = context.ActionDescriptor;
            string? normalizedValue = null;
            if (actionDescriptor.RouteValues.TryGetValue(ActionNameKey, out var value) &&
                !string.IsNullOrEmpty(value))
            {
                normalizedValue = value;
            }

            var stringRouteValue = routeValue?.ToString();
            if (string.Equals(normalizedValue, stringRouteValue, StringComparison.OrdinalIgnoreCase))
            {
                return normalizedValue;
            }

            return stringRouteValue;
        }
    }

    public interface IViewToStringRendererServiceOptions
    {
        string Host { get; }
        string Scheme { get; }
    }
}
