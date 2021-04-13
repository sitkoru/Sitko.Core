using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
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

        protected Task<TResponse> ProcessCall<TRequest, TResponse>(TRequest request,
            ServerCallContext context,
            Func<TResponse, GrpcCallResult> execute)
            where TResponse : class, IGrpcResponse, new() where TRequest : class, IGrpcRequest
        {
            var response = CreateResponse<TResponse>();
            try
            {
                var result = execute(response);
                ProcessResult(result, request, response, context.Method);
            }
            catch (Exception ex)
            {
                ProcessResult(new GrpcCallResult(ex), request, response, context.Method);
            }

            return Task.FromResult(response);
        }

        protected async Task<TResponse> ProcessCallAsync<TRequest, TResponse>(TRequest request,
            ServerCallContext context,
            Func<TResponse, Task<GrpcCallResult>> executeAsync)
            where TResponse : class, IGrpcResponse, new() where TRequest : class, IGrpcRequest
        {
            {
                var response = CreateResponse<TResponse>();
                try
                {
                    var result = await executeAsync(response);
                    ProcessResult(result, request, response, context.Method);
                }
                catch (Exception ex)
                {
                    ProcessResult(new GrpcCallResult(ex), request, response, context.Method);
                }

                return response;
            }
        }

        public async Task ProcessStreamAsync<TRequest, TResponse>(TRequest request,
            IServerStreamWriter<TResponse> responseStream,
            ServerCallContext context,
            Func<Func<Action<TResponse>, Task>, Task> executeAsync)
            where TResponse : class, IGrpcResponse, new() where TRequest : class, IGrpcRequest
        {
            {
                try
                {
                    await executeAsync(async fillResponse =>
                    {
                        var response = CreateResponse<TResponse>();
                        fillResponse(response);
                        await responseStream.WriteAsync(response);
                    });
                }
                catch (Exception ex)
                {
                    var response = CreateResponse<TResponse>();
                    ProcessResult(new GrpcCallResult(ex), request, response, context.Method);
                    await responseStream.WriteAsync(response);
                }
            }
        }

        private void ProcessResult<TRequest, TResponse>(GrpcCallResult result, TRequest request, TResponse response,
            string methodName)
            where TResponse : class, IGrpcResponse, new() where TRequest : class, IGrpcRequest
        {
            if (!result.IsSuccess)
            {
                if (result.Exception is not null)
                {
                    Logger.LogError(result.Exception,
                        "Error in method {MethodName}. Request: {@Request}. Error: {ErrorText}",
                        methodName,
                        request, result.Exception.ToString());
                    response.SetException(result.Exception);
                }

                if (result.Error.Length > 0)
                {
                    response.SetErrors(result.Error);
                }
            }
        }

        protected GrpcCallResult Ok()
        {
            return new();
        }

        protected GrpcCallResult Error(string error)
        {
            return new(error);
        }

        protected GrpcCallResult Error(IEnumerable<string> errors)
        {
            return new(errors);
        }

        protected GrpcCallResult Exception(Exception ex, string? error = null)
        {
            return new(ex, error);
        }
    }
}
