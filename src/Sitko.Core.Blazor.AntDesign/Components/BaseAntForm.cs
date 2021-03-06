﻿using System.Threading.Tasks;
using AntDesign;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OneOf;
using Sitko.Core.App.Blazor.Forms;
using Sitko.Core.App.Localization;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public abstract class BaseAntForm<TEntity, TForm> : BaseFormComponent<TEntity, TForm>
        where TForm : BaseForm<TEntity>
        where TEntity : class, new()
    {
        protected Form<TForm>? AntForm { get; set; }

        [Inject]
        protected ILocalizationProvider<BaseAntForm<TEntity, TForm>> LocalizationProvider { get; set; } = null!;

        [Parameter] public RenderFragment<TForm> ChildContent { get; set; } = null!;

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

        [Inject] protected MessageService MessageService { get; set; } = null!;

        protected override Task ConfigureFormAsync(TForm form)
        {
            form.OnSuccess = () => MessageService.Success(LocalizationProvider["Entity saved successfully"]);
            form.OnError = error => MessageService.Error(error);
            form.OnException = exception => MessageService.Error(exception.ToString());
            return Task.CompletedTask;
        }

        protected Task OnFormErrorAsync(EditContext editContext) =>
            MessageService.Error(string.Join(". ", editContext.GetValidationMessages()));

        public override void Save() => AntForm?.Submit();
    }
}
