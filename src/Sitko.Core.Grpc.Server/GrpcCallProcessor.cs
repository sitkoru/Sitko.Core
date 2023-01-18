using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using PeanutButter.DuckTyping.Extensions;
using Sitko.Core.Grpc.Extensions;
using Sitko.FluentValidation.Graph;

namespace Sitko.Core.Grpc.Server;

public class GrpcCallProcessor
{
    protected static readonly GraphValidationContextOptions ValidationOptions = new()
    {
        NeedToValidate = o =>
            o.GetType().Namespace?.StartsWith("Google.Protobuf",
                StringComparison.InvariantCultureIgnoreCase) != true
    };
}

public class GrpcCallProcessor<TService> : GrpcCallProcessor, IGrpcCallProcessor<TService>
{
    private readonly IFluentGraphValidator graphValidator;
    private readonly ILogger<TService> logger;

    public GrpcCallProcessor(ILogger<TService> logger, IFluentGraphValidator graphValidator)
    {
        this.logger = logger;
        this.graphValidator = graphValidator;
    }

    public async Task<TResponse> ProcessCall<TResponse>(IMessage request, ServerCallContext context,
        Func<TResponse, GrpcCallResult> execute) where TResponse : class, IMessage, new()
    {
        var response = CreateResponse<TResponse>();
        if (!await ValidateRequestAsync(request, response, context))
        {
            return response;
        }

        try
        {
            var result = execute(response);
            ProcessResult(result, request, response, context.Method);
        }
        catch (Exception ex)
        {
            ProcessResult(new GrpcCallResult(ex), request, response, context.Method);
        }

        return response;
    }

    public async Task<TResponse> ProcessCallAsync<TResponse>(IMessage request, ServerCallContext context,
        Func<TResponse, Task<GrpcCallResult>> executeAsync) where TResponse : class, IMessage, new()
    {
        var response = CreateResponse<TResponse>();
        if (!await ValidateRequestAsync(request, response, context))
        {
            return response;
        }

        try
        {
            var result = await executeAsync(response);
            ProcessResult(result, request, response, context.Method);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ProcessResult(new GrpcCallResult(ex), request, response, context.Method);
        }

        return response;
    }

    public Task<TResponse> ProcessCallAsync<TResponse>(IMessage request, ServerCallContext context,
        Func<TResponse, Task> executeAsync) where TResponse : class, IMessage, new() => ProcessCallAsync<TResponse>(
        request, context, async response =>
        {
            await executeAsync(response);
            return GrpcCallResult.Ok();
        });

    public async Task ProcessStreamAsync<TResponse>(IMessage request, IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        Func<Func<Action<TResponse>, Task>, Task> executeAsync) where TResponse : class, IMessage, new()
    {
        var errorResponse = CreateResponse<TResponse>();

        if (!await ValidateRequestAsync(request, errorResponse, context))
        {
            await responseStream.WriteAsync(errorResponse);
            return;
        }

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

    public async Task<TResponse> ProcessStreamAsync<TResponse>(IAsyncStreamReader<IMessage> requestStream,
        ServerCallContext context, Func<TResponse, Task<GrpcCallResult>> executeAsync)
        where TResponse : class, IMessage, new()
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

    public async Task ProcessStreamAsync<TResponse>(IAsyncStreamReader<IMessage> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context, Func<Func<Action<TResponse>, Task>, Task> executeAsync)
        where TResponse : class, IMessage, new()
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

    public async Task<bool> ValidateRequestAsync(IMessage request, IMessage response, ServerCallContext context)
    {
        var validationResult = await graphValidator.TryValidateModelAsync(
            new ModelGraphValidationContext(request, ValidationOptions), context.CancellationToken);
        if (!validationResult.IsValid)
        {
            ProcessResult(
                new GrpcCallResult(validationResult.Results.SelectMany(result =>
                    result.Errors.Select(failure => $"{result.Path}{failure.PropertyName}: {failure.ErrorMessage}"))),
                request,
                response, context.Method);
            return false;
        }

        return true;
    }

    private static TResponse CreateResponse<TResponse>() where TResponse : class, IMessage, new()
    {
        var response = new TResponse();
#pragma warning disable CS0618
        if (response.DuckAs<IGrpcResponse>() is { } grpcResponse)
#pragma warning restore CS0618
        {
            grpcResponse.ResponseInfo = new ApiResponseInfo { IsSuccess = true };
        }

        return response;
    }

    private void ProcessResult<TResponse>(GrpcCallResult result, IMessage? request, TResponse response,
        string methodName)
        where TResponse : class, IMessage
    {
        if (!result.IsSuccess)
        {
            FillErrors(result, request, response, methodName);
        }
    }

    private void FillErrors<TResponse>(GrpcCallResult result, IMessage? request, TResponse response,
        string methodName)
        where TResponse : class, IMessage
    {
        if (result.Exception is not null)
        {
            logger.LogError(result.Exception,
                "Error in method {MethodName}. Request: {@Request}. Error: {ErrorText}",
                methodName,
                request, result.Exception.ToString());
#pragma warning disable CS0618
            if (response.DuckAs<IGrpcResponse>() is { } grpcResponse)
            {
                grpcResponse.SetException(result.Exception);
            }
#pragma warning restore CS0618
        }
        else
        {
            if (result.Error.Length > 0)
            {
#pragma warning disable CS0618
                if (response.DuckAs<IGrpcResponse>() is { } grpcResponse)
                {
                    grpcResponse.SetErrors(result.Error);
                }
#pragma warning restore CS0618
            }
        }
    }
}
