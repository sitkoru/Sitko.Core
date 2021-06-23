using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Sitko.Core.App.Blazor.Forms;
using Sitko.Core.Apps.Blazor.Data.Entities;
using Sitko.Core.Repository;
using Sitko.Core.Storage;

namespace Sitko.Core.Apps.Blazor.Pages
{
    public class BarForm : BaseRepositoryForm<BarModel, Guid>
    {
        public string Bar { get; set; }
        public List<FooModel> Foos { get; set; }
        public StorageItem? StorageItem { get; set; }

        public BarForm(IRepository<BarModel, Guid> repository, ILogger<BarForm> logger) : base(
            repository, logger)
        {
        }

        protected override Task ConfigureQueryAsync(IRepositoryQuery<BarModel> query)
        {
            query.Include(bar => bar.Foos);
            return Task.CompletedTask;
        }

        protected override Task MapEntityAsync(BarModel entity)
        {
            entity.Bar = Bar;
            entity.Foos = Foos;
            entity.StorageItem = StorageItem;
            return Task.CompletedTask;
        }

        protected override Task MapFormAsync(BarModel entity)
        {
            Bar = entity.Bar;
            Foos = entity.Foos;
            StorageItem = entity.StorageItem;
            return Task.CompletedTask;
        }
    }
    
    public class BarFormValidator : AbstractValidator<BarForm>
    {
        public BarFormValidator()
        {
            RuleFor(m => m.Bar).NotEmpty().WithMessage("Blaaaaa");
        }
    }
}
