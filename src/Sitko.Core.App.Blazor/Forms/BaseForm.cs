using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sitko.Core.App.Blazor.Components;

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
        protected TEntity _entity;
        private BaseComponent? _parent;
        public Func<TEntity, Task>? OnSave { get; set; }
        public Func<string, Task>? OnError { get; set; }
        public Func<Task>? OnSuccess { get; set; }
        public Func<Exception, Task>? OnException { get; set; }

        public bool IsLoading { get; set; }

        public Func<TEntity, Task>? AfterSaveAsync { get; set; }
        public Action<TEntity>? AfterSave { get; set; }

        protected abstract TEntity MapEntity(TEntity entity);

        public void SetParent(BaseComponent parent)
        {
            _parent = parent;
        }

        public async Task InitializeAsync(TEntity? entity = null)
        {
            if (entity is null)
            {
                IsNew = true;
                entity = await CreateEntityAsync();
            }

            _entity = entity;
            await MapFormAsync(_entity);
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
            await StartLoadingAsync();
            await BeforeSaveAsync();
            MapEntity(_entity);
            await BeforeEntitySaveAsync(_entity);
            try
            {
                var result = IsNew
                    ? await AddAsync(_entity)
                    : await UpdateAsync(_entity);
                await StopLoadingAsync();
                if (result.IsSuccess)
                {
                    if (IsNew)
                    {
                        await OnCreatedAsync(_entity);
                    }
                    else
                    {
                        await OnUpdatedAsync(_entity);
                    }

                    if (OnSave is not null)
                    {
                        await OnSave(_entity);
                    }

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
