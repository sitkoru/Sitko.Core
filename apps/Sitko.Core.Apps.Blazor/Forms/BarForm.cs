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
    public class BarForm : BaseRepositoryForm<BarModel, Guid>
    {
        public Guid Test { get; set; }
        public string Bar { get; set; } = "";
        public List<FooModel> Foos { get; set; } = new();
        public StorageItem? StorageItem { get; set; }
        public ValueCollection<StorageItem> StorageItems { get; set; } = new();

        public BarForm(IRepository<BarModel, Guid> repository, ILogger<BarForm> logger) : base(
            repository, logger)
        {
        }

        protected override Task ConfigureQueryAsync(IRepositoryQuery<BarModel> query)
        {
            query.Include(bar => bar.Foos);
            return Task.CompletedTask;
        }
    }

    public class BarFormValidator : AbstractValidator<BarForm>
    {
        public BarFormValidator() => RuleFor(m => m.Bar).NotEmpty().WithMessage("Blaaaaa");
    }
}
