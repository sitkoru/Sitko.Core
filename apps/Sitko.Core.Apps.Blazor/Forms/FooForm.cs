using System.Threading.Tasks;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Sitko.Core.App.Blazor.Forms;
using Sitko.Core.Apps.Blazor.Data.Entities;

namespace Sitko.Core.Apps.Blazor.Forms
{
    public class FooForm : BaseForm<FooModel>
    {
        public string Foo { get; set; } = "";


        public FooForm(ILogger<FooForm> logger) : base(logger)
        {
        }

        protected override Task<FormSaveResult> AddAsync(FooModel entity)
        {
            FormSaveResult result = new(true, "");
            return Task.FromResult(result);
        }

        protected override Task<FormSaveResult> UpdateAsync(FooModel entity) =>
            Task.FromResult(new FormSaveResult(true, ""));
    }

    public class FooFormValidator : AbstractValidator<FooForm>
    {
        public FooFormValidator() => RuleFor(m => m.Foo).NotEmpty().WithMessage("Blaaaaa");
    }

    public class FooValidator : AbstractValidator<FooModel>
    {
        public FooValidator() => RuleFor(m => m.Foo).NotEmpty().WithMessage("Blaaaaa");
    }
}
