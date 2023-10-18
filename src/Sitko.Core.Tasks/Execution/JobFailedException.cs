namespace Sitko.Core.Tasks.Execution;

public class JobFailedException : Exception
{
    public JobFailedException(Guid id, string type, string message) : base($"Task {id} ( {type} ) failed: {message}")
    {
    }
}