using MudBlazorUnited.Data.Entities;
using MudBlazorUnited.Data.Repositories;
using Sitko.Core.Blazor.MudBlazorComponents;
using Sitko.Core.Repository;

namespace MudBlazorUnited.Components.Forms
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
    }
}
