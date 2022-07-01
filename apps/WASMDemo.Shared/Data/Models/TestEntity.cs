using Sitko.Core.Repository;

namespace WASMDemo.Shared.Data.Models;

public record TestEntity : EntityRecord<Guid>
{
    public string Text { get; set; } = string.Empty;
}