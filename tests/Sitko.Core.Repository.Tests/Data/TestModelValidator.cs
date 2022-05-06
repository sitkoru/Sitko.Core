using FluentValidation;

namespace Sitko.Core.Repository.Tests.Data;

public class TestModelValidator : AbstractValidator<TestModel>
{
    public TestModelValidator() =>
        RuleFor(m => m.Status).NotEqual(TestStatus.Error).WithMessage("Status can't be error");
}
