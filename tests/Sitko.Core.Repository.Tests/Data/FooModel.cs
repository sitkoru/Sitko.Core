using System.ComponentModel.DataAnnotations.Schema;

namespace Sitko.Core.Repository.Tests.Data;

public class FooModel : Entity<Guid>
{
    public string? FooText { get; set; }
    public Guid BarId { get; set; }
    [ForeignKey(nameof(BarId))] public BarModel? Bar { get; set; } = null!;

    public List<BazModel> BazModels { get; set; } = new();
}

