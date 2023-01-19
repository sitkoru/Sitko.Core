using System.ComponentModel.DataAnnotations.Schema;
using Sitko.Core.Repository.Tests.Data;

namespace Sitko.Core.Repository.EntityFrameworkCore.Tests.Data.TPH;

public abstract class BaseTPHClass : Entity<Guid>
{
    public TPHType Type { get; set; }
    public string Foo { get; set; }
}

public abstract class BaseTPHClass<TConfig> : BaseTPHClass where TConfig : class, new()
{
    [Column(nameof(Config), TypeName = "jsonb")]
    public TConfig Config { get; set; } = new();
}
