using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sitko.Core.Blazor.AntDesign.Server;
using Sitko.Core.Blazor.AntDesignComponents.Components;
using Sitko.Core.Blazor.Forms;
using Sitko.Core.Blazor.MudBlazor.Server;
using Sitko.Core.Blazor.MudBlazorComponents;
using Sitko.Core.Blazor.Server;
using Xunit;

namespace Sitko.Core.App.Blazor.Tests
{
    public class FormValidationTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InvalidNameBlocksSaveAndCorrectedNameClearsErrorsAndChanges(bool useAntDesign)
        {
            var builder = WebApplication.CreateBuilder();
            _ = builder.AddSitkoCoreBlazorServer()
                .AddAntBlazorServer()
                .AddMudBlazorServer();
            _ = builder.Services.AddValidatorsFromAssemblyContaining<FormModelValidator>();

            await using var app = builder.Build();
            await using var scope = app.Services.CreateAsyncScope();
            var navigation = scope.ServiceProvider.GetRequiredService<NavigationManager>();
            ((IHostEnvironmentNavigationManager)navigation).Initialize("http://localhost/", "http://localhost/");
            await using var renderer = new HtmlRenderer(scope.ServiceProvider,
                scope.ServiceProvider.GetRequiredService<ILoggerFactory>());

            var model = new FormModel { Name = "Исходное" };
            BaseForm<FormModel>? form = null;
            EditContext? editContext = null;
            var saves = 0;
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(FormHost.UseAntDesign)] = useAntDesign,
                [nameof(FormHost.Model)] = model,
                [nameof(FormHost.CaptureForm)] = (Action<BaseForm<FormModel>>)(value => form = value),
                [nameof(FormHost.CaptureEditContext)] = (Action<EditContext>)(value => editContext = value),
                [nameof(FormHost.Save)] = (Func<FormModel, Task<FormSaveResult>>)(_ =>
                {
                    saves++;
                    return Task.FromResult(new FormSaveResult(true, null));
                })
            });

            _ = await renderer.Dispatcher.InvokeAsync(() => renderer.RenderComponentAsync<FormHost>(parameters));

            Assert.NotNull(form);
            Assert.NotNull(editContext);
            Assert.False(form.HasChanges);

            var invalid = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            editContext.OnValidationStateChanged += (_, _) =>
            {
                if (editContext.GetValidationMessages().Contains("Имя обязательно."))
                {
                    _ = invalid.TrySetResult();
                }
            };

            await renderer.Dispatcher.InvokeAsync(() =>
            {
                model.Name = "";
                form.NotifyChange(new FieldIdentifier(model, nameof(FormModel.Name)));
            });
            await invalid.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.Contains("Имя обязательно.", form.Errors);
            Assert.True(form.HasChanges);

            await renderer.Dispatcher.InvokeAsync(form.SaveEntityAsync);
            Assert.Equal(0, saves);

            var corrected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            editContext.OnValidationStateChanged += (_, _) =>
            {
                if (!editContext.GetValidationMessages().Any())
                {
                    _ = corrected.TrySetResult();
                }
            };

            await renderer.Dispatcher.InvokeAsync(() =>
            {
                model.Name = "Исправленное";
                form.NotifyChange(new FieldIdentifier(model, nameof(FormModel.Name)));
            });
            await corrected.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.Empty(form.Errors);
            Assert.True(form.HasChanges);
            var change = Assert.Single(form.Changes);
            Assert.Equal(nameof(FormModel.Name), change.Property);
            Assert.Equal("Исходное", change.OriginalValue);
            Assert.Equal("Исправленное", change.CurrentValue);

            await renderer.Dispatcher.InvokeAsync(form.SaveEntityAsync);
            Assert.Equal(1, saves);
            Assert.False(form.HasChanges);
            Assert.Empty(form.Changes);
        }
    }

    public sealed class FormModel
    {
        public string Name { get; set; } = "";
    }

    public sealed class FormModelValidator : AbstractValidator<FormModel>
    {
        public FormModelValidator() => RuleFor(static model => model.Name).NotEmpty().WithMessage("Имя обязательно.");
    }

    internal sealed class RenderedMudForm : BaseMudForm<FormModel>;

    internal sealed class FormHost : ComponentBase
    {
        [Parameter] public bool UseAntDesign { get; set; }
        [Parameter] public FormModel Model { get; set; } = null!;
        [Parameter] public Action<BaseForm<FormModel>> CaptureForm { get; set; } = null!;
        [Parameter] public Action<EditContext> CaptureEditContext { get; set; } = null!;
        [Parameter] public Func<FormModel, Task<FormSaveResult>> Save { get; set; } = null!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (UseAntDesign)
            {
                builder.OpenComponent<AntForm<FormModel>>(0);
                builder.AddAttribute(1, nameof(AntForm<>.ChildContent),
                    (RenderFragment<BaseAntForm<FormModel>>)(_ => CaptureContext));
            }
            else
            {
                builder.OpenComponent<RenderedMudForm>(0);
                builder.AddAttribute(1, nameof(RenderedMudForm.ChildContent),
                    (RenderFragment<FormContext<FormModel>>)(_ => CaptureContext));
            }

            builder.AddAttribute(2, "GetEntity",
                (Func<Task<(bool IsNew, FormModel Entity)>>)(() => Task.FromResult((false, Model))));
            builder.AddAttribute(3, "Add",
                (Func<FormModel, Task<FormSaveResult>>)(_ => throw new InvalidOperationException("Unexpected create")));
            builder.AddAttribute(4, "Update", Save);
            builder.AddComponentReferenceCapture(5, value => CaptureForm((BaseForm<FormModel>)value));
            builder.CloseComponent();
        }

        private void CaptureContext(RenderTreeBuilder builder)
        {
            builder.OpenComponent<EditContextCapture>(0);
            builder.AddAttribute(1, nameof(EditContextCapture.Capture), CaptureEditContext);
            builder.CloseComponent();
        }
    }

    internal sealed class EditContextCapture : ComponentBase
    {
        [CascadingParameter] public EditContext EditContext { get; set; } = null!;
        [Parameter] public Action<EditContext> Capture { get; set; } = null!;

        protected override void OnParametersSet() => Capture(EditContext);
    }
}
