namespace Sitko.Core.Repository.Tests.Data;

public abstract record BaseJsonModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
}

