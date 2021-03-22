using System;

namespace Sitko.Core.Grpc
{
    public class GrpcCallResult
    {
        public bool IsSuccess { get; }
        public string? Error { get; }
        public Exception? Exception { get; }

        public GrpcCallResult()
        {
            IsSuccess = true;
        }

        public GrpcCallResult(string error)
        {
            IsSuccess = false;
            Error = error;
        }

        public GrpcCallResult(Exception exception, string? error = null) : this(error ?? exception.Message)
        {
            Exception = exception;
        }
    }
}
