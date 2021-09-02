using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sitko.Core.Repository.Tests.Data
{
    public class BarModel : Entity<Guid>
    {
        public Guid? TestId { get; set; }
        [ForeignKey(nameof(TestId))] public TestModel? Test { get; set; }

        [InverseProperty(nameof(FooModel.Bar))]
        public List<FooModel> Foos { get; set; } = new();

        public List<BazModel> BazModels { get; set; } = new();

        public List<BaseJsonModel> JsonModels { get; set; } = new();
        public string? Baz { get; set; }
    }
}