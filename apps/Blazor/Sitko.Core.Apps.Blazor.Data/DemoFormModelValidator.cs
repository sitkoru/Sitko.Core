using FluentValidation;

namespace Sitko.Core.Apps.Blazor.Data
{
    public class DemoFormModelValidator : AbstractValidator<DemoFormModel>
    {
        public DemoFormModelValidator() => RuleFor(static model => model.Name).NotEmpty().WithMessage("Введите имя.");
    }
}
