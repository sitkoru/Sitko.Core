using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sitko.Core.Apps.Blazor.Data.Entities;
using Sitko.Core.Apps.Blazor.Data.Repositories;
using Sitko.Core.Blazor.AntDesignComponents.Components;
using Sitko.Core.Repository;

namespace Sitko.Core.Apps.Blazor.Forms;

public class BarForm : BaseAntRepositoryForm<BarModel, Guid, BarRepository>
{
    [Parameter] public RenderFragment<BarForm> ChildContent { get; set; } = null!;

    protected override RenderFragment ChildContentFragment => ChildContent(this);

    protected override Task ConfigureQueryAsync(IRepositoryQuery<BarModel> query)
    {
        query.Include(bar => bar.Foos).Include(bar => bar.Foo);
        return Task.CompletedTask;
    }

    public void SetFoo() => Entity.Foo = new FooModel();

    public void AddFoo() => Entity.Foos.Add(new FooModel());

    public void DeleteFoo() => Entity.Foo = null;
}
