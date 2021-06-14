using System.Threading.Tasks;
using AntDesign;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Sitko.Core.App.Blazor.Forms;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public abstract class BaseAntForm<TEntity, TForm> : BaseFormComponent<TEntity, TForm>
        where TForm : BaseForm<TEntity>
        where TEntity : class, new()
    {
        protected Form<TForm>? AntForm;

        [Parameter] public RenderFragment<TForm> ChildContent { get; set; }

        [Parameter] public bool ValidateOnChange { get; set; } = true;

        [Inject] protected NotificationService NotificationService { get; set; }

        protected override Task ConfigureFormAsync()
        {
            Form.OnSuccess = () => NotificationService.Success(new NotificationConfig
            {
                Message = "Успех",
                Description = "Запись успешно сохранена",
                Placement = NotificationPlacement.BottomRight
            });
            Form.OnError = error => NotificationService.Error(new NotificationConfig
            {
                Message = "Ошибка", Description = error, Placement = NotificationPlacement.BottomRight
            });
            Form.OnException = exception => NotificationService.Error(new NotificationConfig
            {
                Message = "Критическая ошибка",
                Description = exception.ToString(),
                Placement = NotificationPlacement.BottomRight
            });
            return Task.CompletedTask;
        }

        public Task OnFormErrorAsync(EditContext editContext)
        {
            return NotificationService.Error(new NotificationConfig
            {
                Message = "Ошибка", Description = string.Join(". ", editContext.GetValidationMessages())
            });
        }

        public override bool IsValid()
        {
            return AntForm != null && AntForm.Validate();
        }

        public override void Save()
        {
            AntForm?.Submit();
        }
    }
}
