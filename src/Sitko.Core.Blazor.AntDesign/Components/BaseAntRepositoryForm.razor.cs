using System.Threading.Tasks;
using AntDesign;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OneOf;
using Sitko.Core.Repository;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public class AntRepositoryForm<TEntity, TEntityPk> : BaseAntRepositoryForm<TEntity, TEntityPk,
        IRepository<TEntity, TEntityPk>>
        where TEntity : class, IEntity<TEntityPk>, new()
    {
        [Parameter] public RenderFragment<AntRepositoryForm<TEntity, TEntityPk>> ChildContent { get; set; } = null!;

        protected override RenderFragment ChildContentFragment => ChildContent(this);
    }

    public abstract partial class BaseAntRepositoryForm<TEntity, TEntityPk, TRepository>
        where TEntity : class, IEntity<TEntityPk>, new()
        where TRepository : class, IRepository<TEntity, TEntityPk>
    {
        protected Form<TEntity>? AntForm { get; set; }

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

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            OnSuccess ??= () => MessageService.Success(LocalizationProvider["Entity saved successfully"]);
            OnError ??= error => MessageService.Error(error);
            OnException ??= exception => MessageService.Error(exception.ToString());
        }

        protected Task OnFormErrorAsync(EditContext editContext) =>
            MessageService.Error(string.Join(". ", editContext.GetValidationMessages()));

        public void Save() => AntForm?.Submit();

        protected abstract RenderFragment ChildContentFragment { get; }
    }
}
