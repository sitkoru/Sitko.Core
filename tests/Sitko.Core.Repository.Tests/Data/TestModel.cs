using System.ComponentModel.DataAnnotations.Schema;

namespace Sitko.Core.Repository.Tests.Data;

public class TestModel : Entity<Guid>
{
    public int FooId { get; set; }

    public TestStatus Status { get; set; } = TestStatus.Enabled;

    [InverseProperty(nameof(BarModel.Test))]
    public List<BarModel> Bars { get; set; } = new();
}

