using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Sitko.Core.App.Results;

public interface IOperationResult
{
    bool IsSuccess { get; }
    Exception? Exception { get; }
    string? ErrorMessage { get; }
}

[PublicAPI]
public class OperationResult : IOperationResult
{
    public OperationResult() => IsSuccess = true;

    public OperationResult(string errorMessage)
    {
        IsSuccess = false;
        ErrorMessage = errorMessage;
    }

    public OperationResult(Exception exception, string? errorMessage = null) :
        this(errorMessage ?? exception.Message) => Exception = exception;

#if NET5_0_OR_GREATER
    [MemberNotNullWhen(false, nameof(ErrorMessage))]
#endif
    public bool IsSuccess { get; private set; }

    public string? ErrorMessage { get; private set; }
    public Exception? Exception { get; private set; }

    public virtual void SetError(string errorMessage)
    {
        IsSuccess = false;
        ErrorMessage = errorMessage;
    }

    public virtual void SetException(Exception exception)
    {
        SetError(exception.Message);
        Exception = exception;
    }
}

[PublicAPI]
public class OperationResult<T> : IOperationResult
{
    public OperationResult(T result)
    {
        IsSuccess = true;
        Result = result;
    }

    public OperationResult(string errorMessage)
    {
        IsSuccess = false;
        ErrorMessage = errorMessage;
    }

    public OperationResult(Exception exception, string? errorMessage = null) :
        this(errorMessage ?? exception.Message) => Exception = exception;

    public T? Result { get; private set; }

#if NET5_0_OR_GREATER
    [MemberNotNullWhen(true, nameof(Result))]
    [MemberNotNullWhen(false, nameof(ErrorMessage))]
#endif
    public bool IsSuccess { get; private set; }

    public Exception? Exception { get; }
    public string? ErrorMessage { get; }

    public OperationResult<T> SetResult(T result)
    {
        Result = result;
        return this;
    }
}
