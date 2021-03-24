using System;
using System.Collections.Generic;

namespace Sitko.Core.Grpc
{
    public class GrpcCallResult
    {
        public bool IsSuccess { get; }
        private readonly List<string> _errors = new();
        public string[] Error => _errors.ToArray();
        public Exception? Exception { get; }

        public GrpcCallResult()
        {
            IsSuccess = true;
        }

        public GrpcCallResult(string error)
        {
            IsSuccess = false;
            _errors.Add(error);
        }

        public GrpcCallResult(IEnumerable<string> errors)
        {
            IsSuccess = false;
            _errors.AddRange(errors);
        }

        public GrpcCallResult(Exception exception, string? error = null) : this(error ?? exception.Message)
        {
            Exception = exception;
        }
    }
}
