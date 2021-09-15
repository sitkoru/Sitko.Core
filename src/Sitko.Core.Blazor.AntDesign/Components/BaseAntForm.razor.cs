using System.Threading.Tasks;
using AntDesign;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OneOf;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    using System;
    using App.Blazor.Forms;

    public class AntForm<TEntity> : BaseAntForm<TEntity>
        where TEntity : class, new()
    {
#if NET6_0_OR_GREATER
        [EditorRequired]
#endif
        [Parameter] public RenderFragment<BaseAntForm<TEntity>> ChildContent { get; set; } = null!;

        protected override RenderFragment ChildContentFragment => ChildContent(this);
    }

    public abstract partial class BaseAntForm<TEntity> where TEntity : class, new()
    {
        protected Form<TEntity>? AntFormInstance { get; set; }
        protected abstract RenderFragment ChildContentFragment { get; }

        [Parameter] public string Layout { get; set; } = FormLayout.Horizontal;

        [Parameter] public ColLayoutParam LabelCol { get; set; } = new();

        [Parameter] public AntLabelAlignType? LabelAlign { get; set; }

        [Parameter]
        public OneOf<string, int> LabelColSpan
        {
            get => LabelCol.Span;
            set => LabelCol.Span = value;
        }

        [Parameter]
        public OneOf<string, int> LabelColOffset
        {
            get => LabelCol.Offset;
            set => LabelCol.Offset = value;
        }

        [Parameter] public ColLayoutParam WrapperCol { get; set; } = new();

        [Parameter]
        public OneOf<string, int> WrapperColSpan
        {
            get => WrapperCol.Span;
            set => WrapperCol.Span = value;
        }

        [Parameter]
        public OneOf<string, int> WrapperColOffset
        {
            get => WrapperCol.Offset;
            set => WrapperCol.Offset = value;
        }

        [Parameter] public string? Size { get; set; }

        [Parameter] public string? Name { get; set; }

        [Parameter] public bool ValidateOnChange { get; set; } = true;

        [Parameter] public bool Debug { get; set; }

        [Inject] protected MessageService MessageService { get; set; } = null!;

#if NET6_0_OR_GREATER
        [EditorRequired]
#endif
        [Parameter] public Func<TEntity, Task<FormSaveResult>>? Add { get; set; }

#if NET6_0_OR_GREATER
        [EditorRequired]
#endif
        [Parameter] public Func<TEntity, Task<FormSaveResult>>? Update { get; set; }

#if NET6_0_OR_GREATER
        [EditorRequired]
#endif
        [Parameter] public Func<Task<(bool IsNew, TEntity Entity)>>? GetEntity { get; set; }

        protected override Task<(bool IsNew, TEntity Entity)> GetEntityAsync() => GetEntity!();

        protected override Task NotifySuccessAsync()
        {
            MessageService.Success(LocalizationProvider["Entity saved successfully"]);
            return Task.CompletedTask;
        }

        protected override Task NotifyErrorAsync(string errorText)
        {
            MessageService.Error(errorText);
            return Task.CompletedTask;
        }

        protected override Task NotifyExceptionAsync(Exception ex)
        {
            MessageService.Error(ex.ToString());
            return Task.CompletedTask;
        }

        protected Task OnFormErrorAsync(EditContext editContext) =>
            MessageService.Error(string.Join(". ", editContext.GetValidationMessages()));

        public void Save() => AntFormInstance?.Submit();

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            if (GetEntity is null)
            {
                throw new InvalidOperationException("GetEntity is not defined");
            }

            if (Add is null)
            {
                throw new InvalidOperationException("Add is not defined");
            }

            if (Update is null)
            {
                throw new InvalidOperationException("Update is not defined");
            }
        }

        protected override Task<FormSaveResult> AddAsync(TEntity entity) => Add!(entity);

        protected override Task<FormSaveResult> UpdateAsync(TEntity entity) => Update!(entity);

        public override async Task ResetAsync()
        {
            await base.ResetAsync();
            AntFormInstance!.Reset();
        }
    }
}
