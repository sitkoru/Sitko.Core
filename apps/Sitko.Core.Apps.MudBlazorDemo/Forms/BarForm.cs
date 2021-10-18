using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sitko.Core.Apps.MudBlazorDemo.Data.Entities;
using Sitko.Core.Apps.MudBlazorDemo.Data.Repositories;
using Sitko.Core.Blazor.MudBlazorComponents;
using Sitko.Core.Repository;

namespace Sitko.Core.Apps.MudBlazorDemo.Forms
{
    public class BarForm : BaseMudRepositoryForm<BarModel, Guid, BarRepository>
    {
        protected override Task ConfigureQueryAsync(IRepositoryQuery<BarModel> query)
        {
            query.Include(bar => bar.Foos).Include(bar => bar.Foo);
            return Task.CompletedTask;
        }

        public void SetFoo() => Entity.Foo = new FooModel();

        public void AddFoo()
        {
            Entity.Foos.Add(new FooModel());
            NotifyChange();
        }

        public void RemoveFoo(FooModel foo)
        {
            Entity.Foos.Remove(foo);
            NotifyChange();
        }

        public void DeleteFoo() => Entity.Foo = null;

        [Parameter] public RenderFragment<BarForm> ChildContent { get; set; } = null!;

        protected override RenderFragment ChildContentFragment => ChildContent(this);
    }
}
