using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.App.Blazor.Forms
{
    public abstract class BaseForm
    {
    }

    public abstract class BaseForm<TEntity> : BaseForm where TEntity : class
    {
        protected readonly ILogger<BaseForm<TEntity>> Logger;

        protected BaseForm(ILogger<BaseForm<TEntity>> logger)
        {
            Logger = logger;
        }

        public bool IsNew { get; protected set; }
        protected TEntity? Entity;
        private BaseFormComponent? _parent;
        protected bool HasChanges { get; private set; }
        public Func<TEntity, Task>? OnAfterSave { get; set; }
        public Func<TEntity, Task>? OnAfterCreate { get; set; }
        public Func<TEntity, Task>? OnAfterUpdate { get; set; }
        public Func<string, Task>? OnError { get; set; }
        public Func<Task>? OnSuccess { get; set; }
        public Func<Exception, Task>? OnException { get; set; }

        public bool IsLoading { get; set; }

        protected abstract Task MapEntityAsync(TEntity entity);

        public void SetParent(BaseFormComponent parent)
        {
            _parent = parent;
        }

        public void SetEditContext(EditContext editContext)
        {
            EditContext = editContext;
        }

        public void NotifyChange()
        {
            EditContext?.NotifyFieldChanged(new FieldIdentifier(Entity, "Id"));
        }

      
        public async Task InitializeAsync(TEntity? entity = null)
        {
            if (entity is null)
            {
                IsNew = true;
                entity = await CreateEntityAsync();
            }

            Entity = entity;
            await MapFormAsync(Entity);
        }

        protected virtual async Task<TEntity> CreateEntityAsync()
        {
            var entity = Activator.CreateInstance<TEntity>();
            await InitializeEntityAsync(entity);
            return entity;
        }

        protected virtual Task InitializeEntityAsync(TEntity entity)
        {
            return Task.CompletedTask;
        }

        protected abstract Task MapFormAsync(TEntity entity);

        public virtual async Task SaveEntityAsync()
        {
            if (Entity is null)
            {
                throw new Exception("Entity can't be null");
            }

            await StartLoadingAsync();
            await BeforeSaveAsync();
            await MapEntityAsync(Entity);
            await BeforeEntitySaveAsync(Entity);
            try
            {
                var result = IsNew
                    ? await AddAsync(Entity)
                    : await UpdateAsync(Entity);
                await StopLoadingAsync();
                if (result.IsSuccess)
                {
                    if (IsNew)
                    {
                        await OnCreatedAsync(Entity);
                        if (OnAfterCreate is not null)
                        {
                            await OnAfterCreate(Entity);
                        }
                    }
                    else
                    {
                        await OnUpdatedAsync(Entity);
                        if (OnAfterUpdate is not null)
                        {
                            await OnAfterUpdate(Entity);
                        }
                    }

                    if (OnAfterSave is not null)
                    {
                        await OnAfterSave(Entity);
                    }

                    HasChanges = false;
                    await ResetFormAsync();
                    if (OnSuccess is not null)
                    {
                        await OnSuccess();
                    }

                    await NotifyStateChangeAsync();
                }
                else
                {
                    Logger.LogError("Error saving {Entity}: {ErrorText}", typeof(TEntity), result.Error);
                    if (OnError is not null)
                    {
                        await OnError(result.Error!);
                    }
                }
            }
            catch (Exception ex)
            {
                await StopLoadingAsync();
                Logger.LogError(ex, "Error saving {Entity}: {ErrorText}", typeof(TEntity), ex.ToString());
                if (OnException is not null)
                {
                    await OnException(ex);
                }
            }
        }

        private async Task StopLoadingAsync()
        {
            IsLoading = false;
            await NotifyStateChangeAsync();
        }

        private async Task StartLoadingAsync()
        {
            IsLoading = true;
            await NotifyStateChangeAsync();
        }

        private async Task NotifyStateChangeAsync()
        {
            if (_parent is not null)
            {
                await _parent.NotifyStateChangeAsync();
            }
        }

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

        public virtual bool CanSave()
        {
            if (_parent == null)
            {
                return false;
            }

            if (!HasChanges)
            {
                return false;
            }

            return IsValid();
        }

        public virtual bool IsValid()
        {
            return _parent is not null && _parent.IsValid();
        }

        public virtual void Save()
        {
            _parent?.Save();
        }

        public async Task FieldChangedAsync(FieldIdentifier fieldIdentifier)
        {
            await OnFieldChangeAsync(fieldIdentifier);
            HasChanges = await DetectChangesAsync();
        }

        protected virtual Task<bool> DetectChangesAsync()
        {
            return Task.FromResult(true);
        }

        protected virtual Task OnFieldChangeAsync(FieldIdentifier fieldIdentifier)
        {
            return Task.CompletedTask;
        }
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
