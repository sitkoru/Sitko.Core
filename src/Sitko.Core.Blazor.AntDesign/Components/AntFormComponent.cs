using System;
using System.Threading.Tasks;
using AntDesign;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Sitko.Core.App.Blazor.Components;

namespace Sitko.Core.Blazor.AntDesign.Components
{
    public abstract class AntFormComponent<TEntity, TFormModel> : BaseFormComponent<TEntity, TFormModel>
        where TFormModel : BaseFormModel<TEntity> where TEntity : class, new()
    {
        [Inject] protected NotificationService NotificationService { get; set; }

        protected Form<TFormModel>? Form { get; set; }

        protected override Task OnFormErrorAsync(EditContext editContext)
        {
            return NotificationService.Error(new NotificationConfig
            {
                Message = "Ошибка", Description = string.Join(". ", editContext.GetValidationMessages())
            });
        }

        protected override Task NotifySuccessAsync()
        {
            return NotificationService.Success(new NotificationConfig
            {
                Message = "Успех",
                Description = "Запись успешно сохранена",
                Placement = NotificationPlacement.BottomRight
            });
        }

        protected override Task NotifyErrorAsync(string resultError)
        {
            return NotificationService.Error(new NotificationConfig
            {
                Message = "Ошибка", Description = resultError, Placement = NotificationPlacement.BottomRight
            });
        }

        protected override Task NotifyExceptionAsync(Exception exception)
        {
            return NotificationService.Error(new NotificationConfig
            {
                Message = "Критическая ошибка",
                Description = exception.ToString(),
                Placement = NotificationPlacement.BottomRight
            });
        }

        protected void Save()
        {
            Form?.Submit();
        }

        protected override bool CanSave()
        {
            if (Form == null)
            {
                return false;
            }

            return Form.Validate();
        }
    }
}
