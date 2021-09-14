using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.App.Validation
{
    public class FluentGraphValidator
    {
        private readonly ILogger<FluentGraphValidator> logger;
        private readonly IServiceScope serviceScope;

        public FluentGraphValidator(IServiceProvider serviceProvider, ILogger<FluentGraphValidator> logger)
        {
            this.logger = logger;
            serviceScope = serviceProvider.CreateScope();
        }

        private IValidationContext CreateValidationContext(object model, IValidatorSelector? validatorSelector = null)
        {
            validatorSelector ??= ValidatorOptions.Global.ValidatorSelectors.DefaultValidatorSelectorFactory();

            var context = new ValidationContext<object>(model, new PropertyChain(), validatorSelector)
            {
                RootContextData = { ["_FV_ServiceProvider"] = serviceScope.ServiceProvider }
            };

            return context;
        }

        private IValidator? TryGetModelValidator(object model)
        {
            var validatorType = typeof(IValidator<>);
            var formValidatorType = validatorType.MakeGenericType(model.GetType());
            var validator = serviceScope.ServiceProvider.GetService(formValidatorType) as IValidator;
            if (validator is null)
            {
                logger.LogWarning(
                    "FluentValidation.IValidator<{FormType}> is not registered in the application service provider",
                    model.GetType().FullName);
            }

            return validator;
        }

        public async Task<ModelsValidationResult> TryValidateFieldAsync(object model, string fieldName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var validatorSelector = new MemberNameValidatorSelector(new[] { fieldName });
                var validationContext = CreateValidationContext(model, validatorSelector);
                return await TryValidateModelAsync(model, validationContext, cancellationToken);
            }
            catch (Exception ex)
            {
                var msg = $"An unhandled exception occurred when validating field name: '{fieldName}'";

                msg += $" of model of type: '{model.GetType()}'";
                throw new UnhandledValidationException(msg, ex);
            }
        }

        public async Task<ModelsValidationResult> TryValidateModelAsync(object model,
            IValidationContext? validationContext = null, CancellationToken cancellationToken = default)
        {
            try
            {
                validationContext ??= CreateValidationContext(model);
                var result = new ModelsValidationResult();
                var validator = TryGetModelValidator(model);
                if (validator is not null)
                {
                    var validationResult = await validator.ValidateAsync(validationContext, cancellationToken);
                    if (!validationResult.IsValid)
                    {
                        result.Results.Add(new ModelValidationResult(model, validationResult.Errors));
                    }

                    foreach (var property in model.GetType().GetProperties())
                    {
                        var propertyModel = property.GetValue(model);


                        if (propertyModel is not string && propertyModel is IEnumerable enumerable)
                        {
                            foreach (var item in enumerable)
                            {
                                var itemResult = await TryValidateModelAsync(item);
                                if (!itemResult.IsValid)
                                {
                                    foreach (var modelValidationResult in itemResult.Results)
                                    {
                                        result.Results.Add(modelValidationResult);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (propertyModel is null ||
                                propertyModel.GetType().Module.ScopeName == "CommonLanguageRuntimeLibrary" ||
                                propertyModel.GetType().Module.ScopeName.StartsWith("System") ||
                                propertyModel.GetType().Namespace.StartsWith("System") ||
                                propertyModel.GetType().Namespace.StartsWith("Microsoft"))
                            {
                                continue;
                            }

                            var propertyResult =
                                await TryValidateFieldAsync(propertyModel, property.Name, cancellationToken);
                            if (!propertyResult.IsValid)
                            {
                                foreach (var modelValidationResult in propertyResult.Results)
                                {
                                    result.Results.Add(modelValidationResult);
                                }
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                var msg = $"An unhandled exception occurred when validating object of type: '{model.GetType()}'";
                throw new UnhandledValidationException(msg, ex);
            }
        }
    }

    public class ModelsValidationResult
    {
        public bool IsValid => Results.All(r => r.IsValid);
        public HashSet<ModelValidationResult> Results { get; } = new();
    }

    public class ModelValidationResult : IEquatable<ModelValidationResult>
    {
        public object Model { get; }
        public bool IsValid => !Errors.Any();
        public List<ValidationFailure> Errors { get; } = new();

        public ModelValidationResult(object model) => Model = model;

        public ModelValidationResult(object model, IEnumerable<ValidationFailure> errors)
        {
            Model = model;
            Errors.AddRange(errors);
        }

        public bool Equals(ModelValidationResult? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Model.Equals(other.Model);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((ModelValidationResult)obj);
        }

        public override int GetHashCode() => Model.GetHashCode();
    }

    internal class UnhandledValidationException : Exception
    {
        public UnhandledValidationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
