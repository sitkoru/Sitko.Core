using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentValidation;
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

        public async Task SetFooAsync()
        {
            Entity!.Foo = new FooModel();
        }

        public async Task AddFooAsync()
        {
            Entity!.Foos.Add(new FooModel());
        }

        public async Task DeleteFooAsync()
        {
            Entity!.Foo = null;
        }
    }

    public class BarModelValidator : AbstractValidator<BarModel>
    {
        public BarModelValidator() => RuleFor(m => m.Bar).NotEmpty().WithMessage("Blaaaaa");
    }
}
