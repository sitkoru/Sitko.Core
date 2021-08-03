using System;
using Sitko.Core.Repository;

namespace Sitko.Core.Apps.Blazor.Data.Entities
{
    using System.ComponentModel.DataAnnotations.Schema;

    public class FooModel : Entity<Guid>
    {
        public string Foo { get; set; } = "";
    }
}
