using System.Threading.Tasks;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Sitko.Core.App.Blazor.Forms;
using Sitko.Core.Apps.Blazor.Data.Entities;

namespace Sitko.Core.Apps.Blazor.Pages
{
    public class FooForm : BaseForm<FooModel>
    {
        public string Foo { get; set; }


        public FooForm(ILogger<FooForm> logger) : base(logger)
        {
        }

        protected override Task MapEntityAsync(FooModel entity)
        {
            entity.Foo = Foo;
            return Task.CompletedTask;
        }

        protected override Task MapFormAsync(FooModel entity)
        {
            Foo = entity.Foo;
            return Task.CompletedTask;
        }

        protected override Task<FormSaveResult> AddAsync(FooModel entity)
        {
            FormSaveResult result = new(true, "");
            return Task.FromResult(result);
        }

        protected override Task<FormSaveResult> UpdateAsync(FooModel entity)
        {
            return Task.FromResult(new FormSaveResult(true, ""));
        }
    }
    
    public class FooValidator : AbstractValidator<FooForm>
    {
        public FooValidator()
        {
            RuleFor(m => m.Foo).NotEmpty().WithMessage("Blaaaaa");
        }
    }
}
