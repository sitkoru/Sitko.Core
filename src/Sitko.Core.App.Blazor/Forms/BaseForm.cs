using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Sitko.Core.App.Blazor.Components;
using KellermanSoftware.CompareNetObjects;
using Microsoft.AspNetCore.Components;

namespace Sitko.Core.App.Blazor.Forms
{
    using Json;

    public abstract class BaseForm : BaseComponent
    {
        protected EditContext? EditContext { get; set; }

        public override ScopeType ScopeType { get; set; } = ScopeType.Isolated;

        public void SetEditContext(EditContext editContext)
        {
            EditContext = editContext;
            EditContext.OnValidationStateChanged += (_, _) =>
            {
                IsValid = !EditContext.GetValidationMessages().Any();
            };
            EditContext.OnFieldChanged += (_, args) =>
            {
                InvokeAsync(async () =>
                {
                    await OnFieldChangeAsync(args.FieldIdentifier);
                    await UpdateFormStateAsync();
                });
            };
            IsValid = EditContext.Validate();
        }

        protected async Task UpdateFormStateAsync()
        {
            IsValid = EditContext?.GetValidationMessages().Any() == false;
            await DetectChangesAsync();
            await NotifyStateChangeAsync();
        }

        public async Task DetectChangesAsync() => Changes = await GetChangesAsync();

        protected abstract Task<FormChange[]> GetChangesAsync();

        protected virtual Task OnFieldChangeAsync(FieldIdentifier fieldIdentifier) => Task.CompletedTask;

        public abstract void NotifyChange();
        public abstract void NotifyChange(FieldIdentifier fieldIdentifier);

        public abstract Task ResetAsync();
        public abstract bool CanSave();

        public virtual bool IsValid { get; protected set; }
        public bool HasChanges => Changes.Length > 0;
        public FormChange[] Changes { get; private set; } = Array.Empty<FormChange>();
        public string[] Errors => EditContext?.GetValidationMessages().ToArray() ?? Array.Empty<string>();

        public abstract Task SaveEntityAsync();
    }

    public abstract class BaseForm<TEntity> : BaseForm where TEntity : class, new()
    {
        private CompareLogic? comparer;
        protected TEntity? EntitySnapshot { get; private set; }

        public bool IsNew { get; protected set; }
        private TEntity? currentEntity;

        public TEntity Entity
        {
            get => currentEntity ?? throw new InvalidOperationException("Entity is not initialized");
            private set => currentEntity = value;
        }

        private CompareLogic GetComparer()
        {
            if (comparer is null)
            {
                var comparerOptions =
                    new ComparisonConfig { MaxDifferences = 100, Caching = true, AutoClearCache = true };
                ConfigureComparer(comparerOptions);
                comparer = new CompareLogic(comparerOptions);
            }

            return comparer;
        }

        protected virtual void ConfigureComparer(ComparisonConfig comparisonConfig)
        {
        }


        [Parameter] public Func<TEntity, Task>? OnAfterSave { get; set; }
        [Parameter] public Func<TEntity, Task>? OnAfterCreate { get; set; }
        [Parameter] public Func<TEntity, Task>? OnAfterUpdate { get; set; }
        [Parameter] public Func<string, Task>? OnError { get; set; }
        [Parameter] public Func<Task>? OnSuccess { get; set; }
        [Parameter] public Func<Exception, Task>? OnException { get; set; }

        public override void NotifyChange(FieldIdentifier fieldIdentifier) =>
            EditContext?.NotifyFieldChanged(fieldIdentifier);

        public override void NotifyChange() => NotifyChange(new FieldIdentifier(Entity, "Id"));

        protected ILocalizationProvider LocalizationProvider
        {
            get
            {
                if (localizationProvider is null)
                {
                    var localizationProviderType = typeof(ILocalizationProvider<>);
                    var componentLoggerType = localizationProviderType.MakeGenericType(GetType());
                    localizationProvider = GetRequiredService<ILocalizationProvider>(componentLoggerType);
                }

                return localizationProvider;
            }
        }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            (IsNew, Entity) = await GetEntityAsync();
            await InitializeEntityAsync(Entity);
            EntitySnapshot = CreateEntitySnapshot(Entity);
        }

        protected abstract Task<(bool IsNew, TEntity Entity)> GetEntityAsync();

        protected virtual TEntity? CreateEntitySnapshot(TEntity? entity) => JsonHelper.Clone(entity);

        protected virtual Task InitializeEntityAsync(TEntity entity) => Task.CompletedTask;

        public override async Task SaveEntityAsync()
        {
            await StartLoadingAsync();
            await BeforeSaveAsync();
            await BeforeEntitySaveAsync(Entity);
            try
            {
                var result = IsNew
                    ? await AddAsync(Entity)
                    : await UpdateAsync(Entity);
                await StopLoadingAsync();
                if (result.IsSuccess)
                {
                    EntitySnapshot = CreateEntitySnapshot(Entity);
                    await UpdateFormStateAsync();
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


        protected abstract Task<FormSaveResult> AddAsync(TEntity entity);
        protected abstract Task<FormSaveResult> UpdateAsync(TEntity entity);

        protected virtual Task BeforeEntitySaveAsync(TEntity entity) => Task.CompletedTask;

        public override Task ResetAsync() => InitializeAsync();

        protected virtual Task BeforeSaveAsync() => Task.CompletedTask;

        protected virtual Task OnCreatedAsync(TEntity entity) => Task.CompletedTask;

        protected virtual Task OnUpdatedAsync(TEntity entity) => Task.CompletedTask;

        public override bool CanSave() => HasChanges && IsValid;

        protected sealed override async Task<FormChange[]> GetChangesAsync() => await DetectChangesAsync(Entity);

        protected virtual Task<FormChange[]> DetectChangesAsync(TEntity entity)
        {
            var changes = new List<FormChange>();
            if (IsNew || EntitySnapshot is null)
            {
                changes.Add(new FormChange("Entity", null, entity, "null", entity.ToString() ?? "null"));
            }
            else
            {
                var differences = GetComparer().Compare(EntitySnapshot, CreateEntitySnapshot(entity));
                if (!differences.AreEqual)
                {
                    foreach (var difference in differences.Differences)
                    {
                        var change = new FormChange(difference.GetShortItem(), difference.Object1, difference.Object2,
                            difference.Object1Value, difference.Object2Value);
                        changes.Add(change);
                    }
                }
            }

            return Task.FromResult(changes.ToArray());
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

    public record FormChange(string Property, object? OriginalValue, object? CurrentValue, string OriginalValueString,
        string CurrentValueString)
    {
        public override string ToString() => $"{Property}: {OriginalValueString} -> {CurrentValueString}";
    }
}
