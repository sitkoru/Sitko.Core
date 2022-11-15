using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Sitko.Core.Blazor.Components;
using Sitko.FluentValidation.Graph;

namespace Sitko.Core.Blazor.FluentValidation;

/// <summary>
///     Add Fluent Validator support to an EditContext.
/// </summary>
public class FluentValidator : BaseComponent
{
    private ValidationMessageStore? messages;

    /// <summary>
    ///     Inherited object from the FormEdit component.
    /// </summary>
    [CascadingParameter]
    private EditContext CurrentEditContext { get; set; } = null!;

    [Inject] private FluentGraphValidator GraphValidator { get; set; } = null!;

    protected override void Initialize()
    {
        base.Initialize();
        if (CurrentEditContext == null)
        {
            throw new InvalidOperationException($"{nameof(DataAnnotationsValidator)} requires a cascading " +
                                                $"parameter of type {nameof(EditContext)}. For example, you can use {nameof(DataAnnotationsValidator)} " +
                                                "inside an EditForm.");
        }

        AddValidation();
    }

    /// <summary>
    ///     Add form validation logic handlers.
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
    ///     Validate the whole form and trigger client UI update.
    /// </summary>
    private async Task ValidateModel()
    {
        // <EditForm> should now be able to run async validations:
        // https://github.com/dotnet/aspnetcore/issues/11914
        messages ??= new ValidationMessageStore(CurrentEditContext);

        var validationResults = await GraphValidator.TryValidateModelAsync(CurrentEditContext.Model);
        messages.Clear();
        if (!validationResults.IsValid)
        {
            foreach (var modelValidationResult in validationResults.Results)
            {
                foreach (var validationFailure in modelValidationResult.Errors)
                {
                    var fieldID = new FieldIdentifier(modelValidationResult.Model, validationFailure.PropertyName);
                    messages.Add(fieldID, validationFailure.ErrorMessage);
                }
            }
        }

        CurrentEditContext.NotifyValidationStateChanged();
    }

    /// <summary>
    ///     Validate a single field and trigger client UI update.
    /// </summary>
    /// <param name="fieldIdentifier"></param>
    private async Task ValidateField(FieldIdentifier fieldIdentifier)
    {
        messages ??= new ValidationMessageStore(CurrentEditContext);

        var validationResults =
            await GraphValidator.TryValidateFieldAsync(fieldIdentifier.Model, fieldIdentifier.FieldName);
        messages.Clear(fieldIdentifier);
        if (!validationResults.IsValid)
        {
            foreach (var modelValidationResult in validationResults.Results)
            {
                foreach (var validationFailure in modelValidationResult.Errors)
                {
                    var fieldID = new FieldIdentifier(modelValidationResult.Model, validationFailure.PropertyName);
                    messages.Add(fieldID, validationFailure.ErrorMessage);
                }
            }
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
        }
    }
}

