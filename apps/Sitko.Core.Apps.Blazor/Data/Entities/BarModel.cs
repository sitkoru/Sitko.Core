using System;
using System.Collections.Generic;
using Sitko.Core.Apps.Blazor.Pages;
using Sitko.Core.Repository;
using Sitko.Core.Storage;

namespace Sitko.Core.Apps.Blazor.Data.Entities
{
    public class BarModel : Entity<Guid>
    {
        public string Bar { get; set; }

        public List<FooModel> Foos { get; set; } = new();

        public StorageItem? StorageItem { get; set; }
        
        public List<StorageItem> StorageItems { get; set; } = new();
    }
}
