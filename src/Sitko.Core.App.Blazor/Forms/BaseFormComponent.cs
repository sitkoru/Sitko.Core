using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Sitko.Core.App.Blazor.Components;

namespace Sitko.Core.App.Blazor.Forms
{
    public abstract class BaseFormComponent : BaseComponent
    {
        public abstract void Save();
        public abstract EditContext EditContext { set; }
    }

    public abstract class BaseFormComponent<TEntity, TForm> : BaseFormComponent where TForm : BaseForm<TEntity>
        where TEntity : class, new()
    {
        [Inject] public TForm Form { get; private set; } = null!;

        [Parameter] public Func<TEntity, Task>? OnAfterSave { get; set; }
        [Parameter] public Func<TEntity, Task>? OnAfterCreate { get; set; }
        [Parameter] public Func<TEntity, Task>? OnAfterUpdate { get; set; }
        [Parameter] public Func<TForm, Task>? OnInitialize { get; set; }

        public override EditContext EditContext
        {
            set
            {
                Form.SetEditContext(value);
                // ReSharper disable once AsyncVoidLambda
                value.OnFieldChanged += async (_, args) =>
                {
                    await OnFieldChangeAsync(args.FieldIdentifier);
                };
            }
        }

        protected abstract Task ConfigureFormAsync(TForm form);
        protected abstract Task InitializeForm(TForm form);

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            Form.SetParent(this);
            Form.OnAfterSave = entity => OnAfterSave is not null ? OnAfterSave(entity) : Task.CompletedTask;
            Form.OnAfterCreate = entity => OnAfterCreate is not null ? OnAfterCreate(entity) : Task.CompletedTask;
            Form.OnAfterUpdate = entity => OnAfterUpdate is not null ? OnAfterUpdate(entity) : Task.CompletedTask;
            await ConfigureFormAsync(Form);
            await InitializeForm(Form);
            if (OnInitialize is not null)
            {
                await OnInitialize(Form);
            }
        }

        protected virtual async Task OnFieldChangeAsync(FieldIdentifier fieldIdentifier)
        {
            await Form.FieldChangedAsync(fieldIdentifier);
            await NotifyStateChangeAsync();
        }
    }
}
