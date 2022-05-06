using System;
using System.Collections.Generic;

namespace Sitko.Core.Repository.Tests.Data;

public class BazModel : Entity<Guid>
{
    public string Baz { get; set; } = "";
    public List<FooModel> Foos { get; set; } = new();
    public List<BarModel> Bars { get; set; } = new();
}
