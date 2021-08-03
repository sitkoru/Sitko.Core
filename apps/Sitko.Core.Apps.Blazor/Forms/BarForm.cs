using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sitko.Core.App.Blazor.Forms;
using Sitko.Core.App.Collections;
using Sitko.Core.Apps.Blazor.Data.Entities;
using Sitko.Core.Repository;
using Sitko.Core.Storage;

namespace Sitko.Core.Apps.Blazor.Forms
{
    using Core.Blazor.AntDesignComponents.Components;
    using Data.Repositories;
    using Microsoft.EntityFrameworkCore;

    public class BarForm : BaseAntRepositoryForm<BarModel, Guid, BarRepository, BarForm>
    {
        protected override Task ConfigureQueryAsync(IRepositoryQuery<BarModel> query)
        {
            query.Include(bar => bar.Foos).Include(bar => bar.Foo);
            return Task.CompletedTask;
        }

        public void SetFoo() => Entity.Foo = new FooModel();

        public void AddFoo() => Entity.Foos.Add(new FooModel());

        public void DeleteFoo() => Entity.Foo = null;
    }
}
