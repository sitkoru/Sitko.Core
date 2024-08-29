using Sitko.Core.Apps.Blazor.Data.Entities;
using Sitko.Core.Blazor.MudBlazorComponents;
using Sitko.Core.Repository;

namespace Sitko.Core.Apps.Blazor.Client.Components.Forms
{
    public class BarForm : BaseMudRepositoryForm<BarModel, Guid, IRepository<BarModel, Guid>>
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
