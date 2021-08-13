using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentValidation.Internal;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Sitko.Core.App.Blazor.Components;
using Microsoft.Extensions.Logging;


namespace Sitko.Core.Blazor.FluentValidation
{
    /// <summary>
    /// Add Fluent Validator support to an EditContext.
    /// </summary>
    public class FluentValidator : BaseComponent
    {
        private ValidationMessageStore? messages;

        /// <summary>
        /// Inherited object from the FormEdit component.
        /// </summary>
        [CascadingParameter]
        private EditContext CurrentEditContext { get; set; } = null!;

        /// <summary>
        /// The AbstractValidator object for the corresponding form Model object type.
        /// </summary>
        [Parameter]
        public IValidator? Validator { set; get; }

        /// <summary>
        /// The AbstractValidator objects mapping for each children / nested object validators.
        /// </summary>
        [Parameter]
        public Dictionary<Type, IValidator?> ChildValidators { set; get; } = new();

        private IServiceScope ServiceScope { get; set; } = null!;


        protected override void Initialize()
        {
            base.Initialize();
            if (CurrentEditContext == null)
            {
                throw new InvalidOperationException($"{nameof(DataAnnotationsValidator)} requires a cascading " +
                                                    $"parameter of type {nameof(EditContext)}. For example, you can use {nameof(DataAnnotationsValidator)} " +
                                                    "inside an EditForm.");
            }

            ServiceScope = CreateServicesScope();
            AddValidation();
        }

        /// <summary>
        /// Try acquiring the typed validator implementation from the DI.
        /// </summary>
        /// <param name="modelType"></param>
        /// <returns></returns>
        private IValidator? TryGetValidatorForObjectType(Type modelType)
        {
            var validatorType = typeof(IValidator<>);
            var formValidatorType = validatorType.MakeGenericType(modelType);
            return ServiceScope.ServiceProvider.GetService(formValidatorType) as IValidator;
        }

        /// <summary>
        /// Creates an instance of a ValidationContext for an object model.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="validatorSelector"></param>
        /// <returns></returns>
        private IValidationContext CreateValidationContext(object model, IValidatorSelector? validatorSelector = null)
        {
            // This method is required due to breaking changes in FluentValidation 9!
            // https://docs.fluentvalidation.net/en/latest/upgrading-to-9.html#removal-of-non-generic-validate-overload

            validatorSelector ??= ValidatorOptions.Global.ValidatorSelectors.DefaultValidatorSelectorFactory();

            // Don't need to use reflection to construct the context.
            // If you create it as a ValidationContext<object> instead of a ValidationContext<T> then FluentValidation will perform the conversion internally, assuming the types are compatible.
            var context = new ValidationContext<object>(model, new PropertyChain(), validatorSelector)
            {
                RootContextData = { ["_FV_ServiceProvider"] = ServiceScope.ServiceProvider }
            };

            // InjectValidator looks for a service provider inside the ValidationContext with this key.
            return context;
        }

        /// <summary>
        /// Add form validation logic handlers.
        /// </summary>
        private void AddValidation()
        {
            messages = new ValidationMessageStore(CurrentEditContext);

            // Perform object-level validation on request
            // ReSharper disable once AsyncVoidLambda
            CurrentEditContext.OnValidationRequested += HandleModelValidation;

            // Perform per-field validation on each field edit
            // ReSharper disable once AsyncVoidLambda
            CurrentEditContext.OnFieldChanged += HandleFieldValidation;
        }

        private void HandleModelValidation(object? sender, ValidationRequestedEventArgs args) =>
            InvokeAsync(ValidateModel);

        private void HandleFieldValidation(object? sender, FieldChangedEventArgs args) =>
            InvokeAsync(() => ValidateField(args.FieldIdentifier));

        /// <summary>
        /// Validate the whole form and trigger client UI update.
        /// </summary>
        private async Task ValidateModel()
        {
            // <EditForm> should now be able to run async validations:
            // https://github.com/dotnet/aspnetcore/issues/11914
            messages ??= new ValidationMessageStore(CurrentEditContext);

            var validationResults = await TryValidateModel();
            messages.Clear();
            if (validationResults is not null)
            {
                var graph = new ModelGraphCache(CurrentEditContext.Model);
                foreach (var error in validationResults.Errors)
                {
                    var (propertyValue, propertyName) = graph.EvalObjectProperty(error.PropertyName);
                    // while it is impossible to have a validation error for a null child property, better be safe than sorry...
                    if (propertyValue != null)
                    {
                        var fieldID = new FieldIdentifier(propertyValue, propertyName);
                        messages.Add(fieldID, error.ErrorMessage);
                    }
                }
            }

            CurrentEditContext.NotifyValidationStateChanged();
        }

        /// <summary>
        /// Attempts to validate an entire form object model.
        /// </summary>
        /// <returns></returns>
        private async Task<ValidationResult?> TryValidateModel()
        {
            try
            {
                var validationContext = CreateValidationContext(CurrentEditContext.Model);
                var validator = TryGetModelValidator();
                if (validator is null)
                {
                    return null;
                }

                return await validator.ValidateAsync(validationContext);
            }
            catch (Exception ex)
            {
                var msg =
                    $"An unhandled exception occurred when validating <EditForm> model type: '{CurrentEditContext.Model.GetType()}'";
                throw new UnhandledValidationException(msg, ex);
            }
        }

        /// <summary>
        /// Attempts to validate a single field or property of a form model or child object model.
        /// </summary>
        /// <param name="validator"></param>
        /// <param name="fieldIdentifier"></param>
        /// <returns></returns>
        private async Task<ValidationResult> TryValidateField(IValidator validator, FieldIdentifier fieldIdentifier)
        {
            try
            {
                var vselector = new MemberNameValidatorSelector(new[] { fieldIdentifier.FieldName });
                var vctx = CreateValidationContext(fieldIdentifier.Model, validatorSelector: vselector);
                return await validator.ValidateAsync(vctx);
            }
            catch (Exception ex)
            {
                var msg = $"An unhandled exception occurred when validating field name: '{fieldIdentifier.FieldName}'";

                if (CurrentEditContext.Model != fieldIdentifier.Model)
                {
                    msg += $" of a child object of type: {fieldIdentifier.Model.GetType()}";
                }

                msg += $" of <EditForm> model type: '{CurrentEditContext.Model.GetType()}'";
                throw new UnhandledValidationException(msg, ex);
            }
        }

        private IValidator? TryGetModelValidator()
        {
            var validator = Validator ?? TryGetValidatorForObjectType(CurrentEditContext.Model.GetType());
            if (validator is null)
            {
                Logger.LogWarning(
                    "FluentValidation.IValidator<{FormType}> is not registered in the application service provider.",
                    CurrentEditContext.Model.GetType().FullName);
            }

            return validator;
        }

        /// <summary>
        /// Attempts to retrieve the field or property validator of a form model or child object model.
        /// </summary>
        /// <param name="fieldIdentifier"></param>
        /// <returns></returns>
        private IValidator? TryGetFieldValidator(in FieldIdentifier fieldIdentifier)
        {
            if (fieldIdentifier.Model == CurrentEditContext.Model)
            {
                return TryGetModelValidator();
            }

            var modelType = fieldIdentifier.Model.GetType();
            if (ChildValidators.ContainsKey(modelType))
            {
                return ChildValidators[modelType];
            }

            var validator = TryGetValidatorForObjectType(modelType);
            ChildValidators[modelType] = validator;
            return validator;
        }

        /// <summary>
        /// Validate a single field and trigger client UI update.
        /// </summary>
        /// <param name="fieldIdentifier"></param>
        private async Task ValidateField(FieldIdentifier fieldIdentifier)
        {
            messages ??= new ValidationMessageStore(CurrentEditContext);

            var fieldValidator = TryGetFieldValidator(fieldIdentifier);
            if (fieldValidator == null)
            {
                // Should not error / just fail silently for classes not supposed to be validated.
                return;
            }

            var validationResults = await TryValidateField(fieldValidator, fieldIdentifier);
            messages.Clear(fieldIdentifier);

            foreach (var error in validationResults.Errors)
            {
                messages.Add(fieldIdentifier, error.ErrorMessage);
            }

            CurrentEditContext.NotifyValidationStateChanged();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                CurrentEditContext.OnValidationRequested -= HandleModelValidation;
                CurrentEditContext.OnFieldChanged -= HandleFieldValidation;
                ServiceScope.Dispose();
            }
        }
    }
}
