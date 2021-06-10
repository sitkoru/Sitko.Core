using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;

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
        public TFormModel FormModel { get; set; }

        [Parameter] public bool IsNew { get; set; }

        protected TEntity Entity { get; private set; }

        [Parameter] public Func<TEntity, Task>? OnSave { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            Entity = await GetEntityAsync();
            FormModel = await CreateFormModelAsync();
            MarkAsInitialized();
        }

        protected abstract Task<TEntity> GetEntityAsync();

        protected abstract Task<TFormModel> CreateFormModelAsync();
        
        public abstract Task OnFormErrorAsync(EditContext editContext);
        
        public virtual async Task SaveEntityAsync()
        {
            StartLoading();
            await BeforeSaveAsync();
            FormModel.GetEntity(Entity);
            await BeforeEntitySaveAsync(Entity);
            try
            {
                var result = IsNew
                    ? await AddAsync(Entity)
                    : await UpdateAsync(Entity);
                StopLoading();
                if (result.IsSuccess)
                {
                    if (IsNew)
                    {
                        await OnCreatedAsync(Entity);
                    }
                    else
                    {
                        await OnUpdatedAsync(Entity);
                    }

                    if (OnSave is not null)
                    {
                        await OnSave(Entity);
                    }
                    await ResetFormAsync();
                    await NotifySuccessAsync();
                    StateHasChanged();
                }
                else
                {
                    Logger.LogError("Error saving model {Entity}: {ErrorText}", typeof(TEntity), result.Error!);
                    await NotifyErrorAsync(result.Error!);
                }
            }
            catch (Exception ex)
            {
                StopLoading();
                Logger.LogError(ex, "Error saving model {Entity}: {ErrorText}", typeof(TEntity), ex.ToString());
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
        public abstract TEntity GetEntity(TEntity entity);
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
