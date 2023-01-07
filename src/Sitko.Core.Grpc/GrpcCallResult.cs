namespace Sitko.Core.Grpc;

public class GrpcCallResult
{
    private readonly List<string> errors = new();

    public GrpcCallResult() => IsSuccess = true;

    public GrpcCallResult(string error)
    {
        IsSuccess = false;
        errors.Add(error);
    }

    public GrpcCallResult(IEnumerable<string> errors)
    {
        IsSuccess = false;
        this.errors.AddRange(errors);
    }

    public GrpcCallResult(Exception exception, string? error = null) : this(error ?? exception.Message) =>
        Exception = exception;

    public bool IsSuccess { get; }
    public string[] Error => errors.ToArray();
    public Exception? Exception { get; }

    public static GrpcCallResult Ok() => new();
}
