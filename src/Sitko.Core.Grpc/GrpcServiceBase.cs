using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Sitko.Core.App.Results;
using Sitko.Core.Grpc.Extensions;

namespace Sitko.Core.Grpc;

[PublicAPI]
public abstract class GrpcServiceBase : IGrpcService
{
    protected GrpcServiceBase(ILogger<GrpcServiceBase> logger) => Logger = logger;
    protected ILogger<GrpcServiceBase> Logger { get; }

    [PublicAPI]
    protected TResponse CreateResponse<TResponse>() where TResponse : class, IGrpcResponse, new() =>
        new() { ResponseInfo = new ApiResponseInfo { IsSuccess = true } };

    protected Task<TResponse> ProcessCall<TResponse>(IGrpcRequest request,
        ServerCallContext context,
        Func<TResponse, GrpcCallResult> execute)
        where TResponse : class, IGrpcResponse, new()
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

    protected async Task<TResponse> ProcessCallAsync<TResponse>(IGrpcRequest request,
        ServerCallContext context,
        Func<TResponse, Task<GrpcCallResult>> executeAsync)
        where TResponse : class, IGrpcResponse, new()
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

    public async Task ProcessStreamAsync<TResponse>(IGrpcRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        Func<Func<Action<TResponse>, Task>, Task> executeAsync)
        where TResponse : class, IGrpcResponse, new()
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

    public async Task<TResponse> ProcessStreamAsync<TResponse>(IAsyncStreamReader<IGrpcRequest> requestStream,
        ServerCallContext context,
        Func<TResponse, Task<GrpcCallResult>> executeAsync)
        where TResponse : class, IGrpcResponse, new()
    {
        var response = CreateResponse<TResponse>();
        try
        {
            var result = await executeAsync(response);
            ProcessResult(result, null, response, context.Method);
        }
        catch (Exception ex)
        {
            ProcessResult(new GrpcCallResult(ex), null, response, context.Method);
        }

        return response;
    }

    public async Task ProcessStreamAsync<TResponse>(IAsyncStreamReader<IGrpcRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        Func<Func<Action<TResponse>, Task>, Task> executeAsync)
        where TResponse : class, IGrpcResponse, new()
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
            ProcessResult(new GrpcCallResult(ex), null, response, context.Method);
            await responseStream.WriteAsync(response);
        }
    }

    private void ProcessResult<TResponse>(GrpcCallResult result, IGrpcRequest? request, TResponse response,
        string methodName)
        where TResponse : class, IGrpcResponse, new()
    {
        if (!result.IsSuccess)
        {
            FillErrors(result, request, response, methodName);
        }
    }

    protected void FillErrors<TResponse>(GrpcCallResult result, IGrpcRequest? request, TResponse response,
        string methodName)
        where TResponse : class, IGrpcResponse, new()
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
            Logger.LogError("Error in method {MethodName}. Request: {@Request}. Errors: {ErrorText}",
                methodName,
                request, string.Join(", ", result.Error));
            response.SetErrors(result.Error);
        }
    }

    protected GrpcCallResult Ok() => new();

    protected GrpcCallResult Error(string error) => new(error);

    protected GrpcCallResult Error(IEnumerable<string> errors) => new(errors);

    protected GrpcCallResult Exception(Exception ex, string? error = null) => new(ex, error);

    protected GrpcCallResult Result(IOperationResult result)
    {
        if (result.IsSuccess)
        {
            return Ok();
        }

        return result.Exception is not null
            ? Exception(result.Exception, result.ErrorMessage)
            : Error(result.ErrorMessage!);
    }
}
