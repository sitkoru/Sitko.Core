using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App.Validation;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.App.Tests
{
    public class FluentGraphValidatorTests : BaseTest<ValidationTestScope>
    {
        public FluentGraphValidatorTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task ValidateParent()
        {
            var scope = await GetScopeAsync();
            var validator = scope.GetService<FluentGraphValidator>();
            var foo = new FooModel();
            var result = await validator.TryValidateModelAsync(foo);
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Results.Should().ContainSingle();
            var fooResult = result.Results.First();
            fooResult.Model.Should().Be(foo);
            fooResult.Errors.Should().HaveCount(2);
            fooResult.Errors.Should().Contain(failure => failure.PropertyName == nameof(FooModel.Id));
            fooResult.Errors.Should().Contain(failure => failure.PropertyName == nameof(FooModel.BarModels));
        }

        [Fact]
        public async Task ValidateChild()
        {
            var scope = await GetScopeAsync();
            var validator = scope.GetService<FluentGraphValidator>();
            var bar = new BarModel();
            var foo = new FooModel
            {
                Id = Guid.NewGuid(),
                BarModels = new List<BarModel>
                {
                    bar
                }
            };
            var result = await validator.TryValidateModelAsync(foo);
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Results.Should().ContainSingle();
            var fooResult = result.Results.First();
            fooResult.Model.Should().Be(bar);
            fooResult.Errors.Should().HaveCount(1);
            fooResult.Errors.Should().Contain(failure => failure.PropertyName == nameof(BarModel.TestGuid));
        }
    }

    [UsedImplicitly]
    public class ValidationTestScope : BaseTestScope
    {
        protected override IServiceCollection ConfigureServices(IConfiguration configuration,
            IHostEnvironment environment,
            IServiceCollection services, string name)
        {
            base.ConfigureServices(configuration, environment, services, name);
            services.AddValidatorsFromAssemblyContaining<FluentGraphValidatorTests>();
            return services;
        }
    }

    public class FooModel
    {
        public Guid Id { get; set; } = Guid.Empty;
        public List<BarModel> BarModels { get; set; } = new();
    }

    public class BarModel
    {
        public Guid TestGuid { get; set; } = Guid.Empty;
    }

    [UsedImplicitly]
    public class FooModelValidator : AbstractValidator<FooModel>
    {
        public FooModelValidator()
        {
            RuleFor(f => f.Id).NotEmpty();
            RuleFor(f => f.BarModels).NotEmpty();
        }
    }

    [UsedImplicitly]
    public class BarModelValidator : AbstractValidator<BarModel>
    {
        public BarModelValidator() => RuleFor(b => b.TestGuid).NotEmpty();
    }
}
