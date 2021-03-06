﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Sitko.Core.App.Json;
using AutoMapper;

namespace Sitko.Core.App.Blazor.Forms
{
    public abstract class BaseForm
    {
        protected BaseFormComponent? Parent { get; set; }
        protected EditContext? EditContext { get; set; }

        public void SetParent(BaseFormComponent parent) => Parent = parent;

        public void SetEditContext(EditContext editContext)
        {
            EditContext = editContext;
            EditContext.OnValidationStateChanged += (_, _) =>
            {
                IsValid = !EditContext.GetValidationMessages().Any();
            };
        }

        public abstract void NotifyChange();
        public abstract void NotifyChange(FieldIdentifier fieldIdentifier);

        public abstract Task ResetAsync();
        public abstract bool CanSave();

        public virtual bool IsValid { get; protected set; }

        public virtual void Save() => Parent?.Save();

        public abstract Task FieldChangedAsync(FieldIdentifier fieldIdentifier);
        public abstract Task SaveEntityAsync();
    }

    public abstract class BaseForm<TEntity> : BaseForm where TEntity : class
    {
        protected ILogger<BaseForm<TEntity>> Logger { get; }

        private string? oldEntityJson;
        private readonly Mapper mapper;

        protected BaseForm(ILogger<BaseForm<TEntity>> logger)
        {
            Logger = logger;
            var mapperConfiguration = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap(typeof(TEntity), GetType());
                cfg.CreateMap(GetType(), typeof(TEntity));
            });
            mapper = new Mapper(mapperConfiguration);
        }

        public bool IsNew { get; protected set; }
        protected TEntity? Entity { get; private set; }

        protected bool HasChanges { get; private set; }
        public Func<TEntity, Task>? OnAfterSave { get; set; }
        public Func<TEntity, Task>? OnAfterCreate { get; set; }
        public Func<TEntity, Task>? OnAfterUpdate { get; set; }
        public Func<string, Task>? OnError { get; set; }
        public Func<Task>? OnSuccess { get; set; }
        public Func<Exception, Task>? OnException { get; set; }

        public bool IsLoading { get; set; }

        protected virtual Task MapEntityAsync(TEntity entity)
        {
            mapper.Map(this, Entity);
            return Task.CompletedTask;
        }

        public override void NotifyChange(FieldIdentifier fieldIdentifier) =>
            EditContext?.NotifyFieldChanged(fieldIdentifier);

        public override void NotifyChange() => NotifyChange(new FieldIdentifier(Entity!, "Id"));

        public async Task InitializeAsync(TEntity? entity = null)
        {
            if (entity is null)
            {
                IsNew = true;
                entity = await CreateEntityAsync();
            }

            Entity = entity;
            oldEntityJson = JsonHelper.SerializeWithMetadata(entity);
            await MapFormAsync(Entity);
        }

        protected virtual async Task<TEntity> CreateEntityAsync()
        {
            var entity = Activator.CreateInstance<TEntity>();
            await InitializeEntityAsync(entity);
            return entity;
        }

        protected virtual Task InitializeEntityAsync(TEntity entity) => Task.CompletedTask;

        protected virtual Task MapFormAsync(TEntity entity)
        {
            mapper.Map(Entity, this);
            return Task.CompletedTask;
        }

        public override async Task SaveEntityAsync()
        {
            if (Entity is null)
            {
                throw new InvalidOperationException("Entity can't be null");
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
                    HasChanges = false;
                    oldEntityJson = JsonHelper.SerializeWithMetadata(Entity);
                    await NotifyStateChangeAsync();
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

        protected async Task NotifyStateChangeAsync()
        {
            if (Parent is not null)
            {
                await Parent.NotifyStateChangeAsync();
            }
        }

        protected abstract Task<FormSaveResult> AddAsync(TEntity entity);
        protected abstract Task<FormSaveResult> UpdateAsync(TEntity entity);

        protected virtual Task BeforeEntitySaveAsync(TEntity entity) => Task.CompletedTask;

        public override Task ResetAsync() => InitializeAsync(Entity);

        protected virtual Task BeforeSaveAsync() => Task.CompletedTask;

        protected virtual Task OnCreatedAsync(TEntity entity) => Task.CompletedTask;

        protected virtual Task OnUpdatedAsync(TEntity entity) => Task.CompletedTask;

        public override bool CanSave()
        {
            if (Parent == null)
            {
                return false;
            }

            return HasChanges && IsValid;
        }

        public override async Task FieldChangedAsync(FieldIdentifier fieldIdentifier)
        {
            await OnFieldChangeAsync(fieldIdentifier);
            HasChanges = await DetectChangesAsync();
        }

        private async Task<bool> DetectChangesAsync()
        {
            if (Entity is not null && !IsNew)
            {
                await MapEntityAsync(Entity);
                return await DetectChangesAsync(Entity);
            }

            return true;
        }

        protected virtual Task<bool> DetectChangesAsync(TEntity entity)
        {
            var newJson = JsonHelper.SerializeWithMetadata(entity);
            return Task.FromResult(!newJson.Equals(oldEntityJson, StringComparison.Ordinal));
        }

        protected virtual Task OnFieldChangeAsync(FieldIdentifier fieldIdentifier) => Task.CompletedTask;
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
