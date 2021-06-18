using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Sitko.Core.App.Blazor.Components;

namespace Sitko.Core.App.Blazor.Forms
{
    public abstract class BaseFormComponent : BaseComponent
    {
        protected abstract Task InitializeForm();
        protected abstract Task ConfigureFormAsync();
        public abstract bool IsValid();
        public abstract void Save();
        public abstract Task OnFieldChangeAsync(FieldIdentifier fieldIdentifier);
    }

    public abstract class BaseFormComponent<TEntity, TForm> : BaseFormComponent where TForm : BaseForm<TEntity>
        where TEntity : class, new()
    {
        public TForm Form { get; private set; }

        [Parameter] public Func<TEntity, Task>? OnAfterSave { get; set; }
        [Parameter] public Func<TEntity, Task>? OnAfterCreate { get; set; }
        [Parameter] public Func<TEntity, Task>? OnAfterUpdate { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            Form = GetService<TForm>();
            Form.SetParent(this);
            Form.OnAfterSave = entity => OnAfterSave is not null ? OnAfterSave(entity) : Task.CompletedTask;
            Form.OnAfterCreate = entity => OnAfterCreate is not null ? OnAfterCreate(entity) : Task.CompletedTask;
            Form.OnAfterUpdate = entity => OnAfterUpdate is not null ? OnAfterUpdate(entity) : Task.CompletedTask;
            await ConfigureFormAsync();
            await InitializeForm();
            MarkAsInitialized();
        }

        public override async Task OnFieldChangeAsync(FieldIdentifier fieldIdentifier)
        {
            await Form.FieldChangedAsync(fieldIdentifier);
            await NotifyStateChangeAsync();
        }
    }
}
