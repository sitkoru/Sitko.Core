using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Sitko.Core.App.Blazor.Components
{
    public abstract class BaseFormComponent : BaseComponent
    {
        public Task NotifyStateChangeAsync()
        {
            return InvokeAsync(StateHasChanged);
        }
    }

    public abstract class BaseFormComponent<TEntity, TFormModel> : BaseFormComponent
        where TEntity : class, new()
        where TFormModel : BaseFormModel<TEntity>
    {
        protected TFormModel FormModel { get; set; }
        protected bool IsNew { get; private set; }

        [Parameter] public TEntity? Entity { get; set; }

        [Parameter] public Func<TEntity, Task>? OnSave { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            if (Entity is null)
            {
                IsNew = true;
            }

            FormModel = await CreateFormModelAsync();
            MarkAsInitialized();
        }

        protected abstract Task<TFormModel> CreateFormModelAsync();

        protected abstract Task OnFormErrorAsync(EditContext editContext);

        protected async Task SaveEntityAsync()
        {
            StartLoading();
            await BeforeSaveAsync();
            var entity = FormModel.GetEntity();
            await BeforeEntitySaveAsync(entity);
            try
            {
                var result = IsNew
                    ? await AddAsync(entity)
                    : await UpdateAsync(entity);
                StopLoading();
                if (result.IsSuccess)
                {
                    await ResetFormAsync();
                    if (IsNew)
                    {
                        await OnCreatedAsync(entity);
                        if (OnSave is not null)
                        {
                            await OnSave(entity);
                        }

                        StateHasChanged();
                    }
                    else
                    {
                        await OnUpdatedAsync(entity);
                        await NotifySuccessAsync();
                    }
                }
                else
                {
                    await NotifyErrorAsync(result.Error!);
                }
            }
            catch (Exception ex)
            {
                StopLoading();
                await NotifyExceptionAsync(ex);
            }
        }

        protected abstract Task NotifyExceptionAsync(Exception exception);

        protected abstract Task NotifySuccessAsync();

        protected abstract Task NotifyErrorAsync(string resultError);

        protected abstract Task<FormSaveResult> AddAsync(TEntity entity);
        protected abstract Task<FormSaveResult> UpdateAsync(TEntity entity);

        protected virtual Task BeforeEntitySaveAsync(TEntity entity)
        {
            return Task.CompletedTask;
        }

        protected virtual Task ResetFormAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual Task BeforeSaveAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnCreatedAsync(TEntity entity)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnUpdatedAsync(TEntity entity)
        {
            return Task.CompletedTask;
        }

        public Func<TFormModel, Task>? AfterSaveAsync { get; set; }
        public Action<TFormModel>? AfterSave { get; set; }

        protected virtual bool CanSave()
        {
            return true;
        }
    }

    public abstract class BaseFormModel<TEntity> where TEntity : class
    {
        public abstract TEntity GetEntity();
    }

    public class FormSaveResult
    {
        public FormSaveResult(bool isSuccess, string? error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        public bool IsSuccess { get; }
        public string? Error { get; }
    }
}
