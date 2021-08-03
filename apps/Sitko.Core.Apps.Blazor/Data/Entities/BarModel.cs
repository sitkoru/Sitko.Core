using System;
using System.Collections.Generic;
using Sitko.Core.App.Collections;
using Sitko.Core.Repository;
using Sitko.Core.Storage;

namespace Sitko.Core.Apps.Blazor.Data.Entities
{
    using System.ComponentModel.DataAnnotations.Schema;

    public class BarModel : Entity<Guid>
    {
        public string Bar { get; set; } = "";

        public List<FooModel> Foos { get; set; } = new();
        public Guid? FooId { get; set; }
        [ForeignKey(nameof(FooId))] public FooModel? Foo { get; set; }
        public DateTimeOffset Date { get; set; } = DateTimeOffset.UtcNow;
        public decimal Sum { get; set; } = 100;

        public StorageItem? StorageItem { get; set; }

        public ValueCollection<StorageItem> StorageItems { get; set; } = new();
    }
}
