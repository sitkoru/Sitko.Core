using Sitko.Core.Repository;

namespace Sitko.Core.Tasks.Data.Entities;

public record BaseTask : EntityRecord<Guid>, IBaseTask
{
    public DateTimeOffset DateAdded { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset DateUpdated { get; set; } = DateTimeOffset.UtcNow;
    public TaskStatus TaskStatus { get; set; } = TaskStatus.Wait;
    public DateTimeOffset? ExecuteDateStart { get; set; }
    public DateTimeOffset? ExecuteDateEnd { get; set; }
#pragma warning disable CS8618
    public string Type { get; set; }
#pragma warning restore CS8618
    public Guid? ParentId { get; set; }
    public string? UserId { get; set; }
    public DateTimeOffset? LastActivityDate { get; set; }
    public virtual string GetKey() => $"{Type}_{Guid.NewGuid()}";
}
