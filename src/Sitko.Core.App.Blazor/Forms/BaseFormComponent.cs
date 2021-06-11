using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sitko.Core.App.Blazor.Components;

namespace Sitko.Core.App.Blazor.Forms
{
    public abstract class BaseFormComponent<TEntity, TForm> : BaseComponent where TForm : BaseForm<TEntity>
        where TEntity : class, new()
    {
        public TForm Form { get; private set; }
        protected abstract Task InitializeForm();

        [Parameter] public Func<TEntity, Task>? OnSave { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            Form = GetService<TForm>();
            Form.SetParent(this);
            Form.OnSave = entity => OnSave is not null ? OnSave(entity) : Task.CompletedTask;
            await ConfigureFormAsync();
            await InitializeForm();
            MarkAsInitialized();
        }

        protected abstract Task ConfigureFormAsync();
    }
}
