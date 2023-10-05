namespace Sitko.Core.Tasks.Data.Entities;

public record BaseTaskResult
{
    public bool IsSuccess { get; set; } = true;
    public bool HasWarnings { get; set; }
    public string? ErrorMessage { get; set; }
}