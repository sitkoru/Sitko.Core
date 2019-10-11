using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Sitko.Core.Metrics;

namespace Sitko.Core.Grpc
{
    public class ServiceMetricsInterceptor : Interceptor
    {
        private readonly IMetricsCollector _metricsCollector;

        public ServiceMetricsInterceptor(IMetricsCollector metricsCollector)
        {
            _metricsCollector = metricsCollector;
        }

        public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
            IAsyncStreamReader<TRequest> requestStream, ServerCallContext context,
            ClientStreamingServerMethod<TRequest, TResponse> continuation)
        {
            var collector = GetMetricsCollector(context.Host, context.Method, CollectorMode.Server);
            try
            {
                return await continuation(requestStream, context);
            }
            catch (RpcException e)
            {
                collector?.SetStatusCode(e.Status.StatusCode);
                throw;
            }
            finally
            {
                collector?.End();
            }
        }

        public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
            IAsyncStreamReader<TRequest> requestStream,
            IServerStreamWriter<TResponse> responseStream, ServerCallContext context,
            DuplexStreamingServerMethod<TRequest, TResponse> continuation)
        {
            var collector = GetMetricsCollector(context.Host, context.Method, CollectorMode.Server);
            try
            {
                await continuation(requestStream, responseStream, context);
            }
            catch (RpcException e)
            {
                collector?.SetStatusCode(e.Status.StatusCode);
                throw;
            }
            finally
            {
                collector?.End();
            }
        }

        public override async Task ServerStreamingServerHandler<TRequest, TResponse>(TRequest request,
            IServerStreamWriter<TResponse> responseStream,
            ServerCallContext context, ServerStreamingServerMethod<TRequest, TResponse> continuation)
        {
            var collector = GetMetricsCollector(context.Host, context.Method, CollectorMode.Server);
            try
            {
                await continuation(request, responseStream, context);
            }
            catch (RpcException e)
            {
                collector?.SetStatusCode(e.Status.StatusCode);
                throw;
            }
            finally
            {
                collector?.End();
            }
        }

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request,
            ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            var collector = GetMetricsCollector(context.Host, context.Method, CollectorMode.Server);
            try
            {
                return await continuation(request, context);
            }
            catch (RpcException e)
            {
                collector?.SetStatusCode(e.Status.StatusCode);
                throw;
            }
            finally
            {
                collector?.End();
            }
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            var collector = GetMetricsCollector(context.Method.ServiceName, context.Method.Name, CollectorMode.Client);
            try
            {
                return base.AsyncUnaryCall(request, context, continuation);
            }
            catch (RpcException e)
            {
                collector?.SetStatusCode(e.Status.StatusCode);
                throw;
            }
            finally
            {
                collector?.End();
            }
        }

        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            var collector = GetMetricsCollector(context.Method.ServiceName, context.Method.Name, CollectorMode.Client);
            try
            {
                return base.AsyncClientStreamingCall(context, continuation);
            }
            catch (RpcException e)
            {
                collector?.SetStatusCode(e.Status.StatusCode);
                throw;
            }
            finally
            {
                collector?.End();
            }
        }

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            var collector = GetMetricsCollector(context.Method.ServiceName, context.Method.Name, CollectorMode.Client);
            try
            {
                return base.BlockingUnaryCall(request, context, continuation);
            }
            catch (RpcException e)
            {
                collector?.SetStatusCode(e.Status.StatusCode);
                throw;
            }
            finally
            {
                collector?.End();
            }
        }

        public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            var collector = GetMetricsCollector(context.Method.ServiceName, context.Method.Name, CollectorMode.Client);
            try
            {
                return base.AsyncDuplexStreamingCall(context, continuation);
            }
            catch (RpcException e)
            {
                collector?.SetStatusCode(e.Status.StatusCode);
                throw;
            }
            finally
            {
                collector?.End();
            }
        }

        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            var collector = GetMetricsCollector(context.Method.ServiceName, context.Method.Name, CollectorMode.Client);
            try
            {
                return base.AsyncServerStreamingCall(request, context, continuation);
            }
            catch (RpcException e)
            {
                collector?.SetStatusCode(e.Status.StatusCode);
                throw;
            }
            finally
            {
                collector?.End();
            }
        }

        private GrpcMetricsCollector GetMetricsCollector(string serviceName, string methodName, CollectorMode mode)
        {
            return GrpcMetricsCollector.Begin(_metricsCollector, serviceName, methodName, mode);
        }
    }
}
