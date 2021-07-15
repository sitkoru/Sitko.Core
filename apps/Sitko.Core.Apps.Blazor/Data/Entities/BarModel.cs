using System;
using System.Collections.Generic;
using Sitko.Core.App.Collections;
using Sitko.Core.Repository;
using Sitko.Core.Storage;

namespace Sitko.Core.Apps.Blazor.Data.Entities
{
    public class BarModel : Entity<Guid>
    {
        public string Bar { get; set; } = "";

        public List<FooModel> Foos { get; set; } = new();
        public DateTimeOffset Date { get; set; } = DateTimeOffset.UtcNow;

        public StorageItem? StorageItem { get; set; }

        public ValueCollection<StorageItem> StorageItems { get; set; } = new();
    }
}
