using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sitko.Core.Grpc.Extensions;

namespace Sitko.Core.Grpc
{
    public abstract class GrpcServiceBase : IGrpcService
    {
        protected ILogger<GrpcServiceBase> Logger { get; }

        protected GrpcServiceBase(ILogger<GrpcServiceBase> logger)
        {
            Logger = logger;
        }

        protected TResponse CreateResponse<TResponse>() where TResponse : class, IGrpcResponse, new()
        {
            return new() {ResponseInfo = new ApiResponseInfo {IsSuccess = true}};
        }

        protected TResponse ProcessCall<TRequest, TResponse>(TRequest request,
            Func<TResponse, GrpcCallResult> execute,
            [CallerMemberName] string? methodName = null)
            where TResponse : class, IGrpcResponse, new() where TRequest : class, IGrpcRequest
        {
            var response = CreateResponse<TResponse>();
            try
            {
                var result = execute(response);
                ProcessError(result, request, response);
            }
            catch (Exception ex)
            {
                response.SetException(ex, Logger, request, 500, methodName);
            }

            return response;
        }

        private void ProcessError<TRequest, TResponse>(GrpcCallResult result, TRequest request, TResponse response)
            where TResponse : class, IGrpcResponse, new() where TRequest : class, IGrpcRequest
        {
            if (!result.IsSuccess)
            {
                if (result.Exception is not null)
                {
                    response.SetException(result.Exception, Logger, request);
                }
                else
                {
                    response.SetError(result.Error!);
                }
            }
        }

        protected async Task<TResponse> ProcessCallAsync<TRequest, TResponse>(TRequest request,
            Func<TResponse, Task<GrpcCallResult>> executeAsync,
            ILogger? logger = null,
            [CallerMemberName] string? methodName = null)
            where TResponse : class, IGrpcResponse, new() where TRequest : class, IGrpcRequest
        {
            {
                var response = CreateResponse<TResponse>();
                try
                {
                    var result = await executeAsync(response);
                    ProcessError(result, request, response);
                }
                catch (Exception ex)
                {
                    response.SetException(ex, logger, request, 500, methodName);
                }

                return response;
            }
        }
    }
}
