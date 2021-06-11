using System.Threading.Tasks;
using Sitko.Core.App.Blazor.Components;

namespace Sitko.Core.App.Blazor.Forms
{
    public abstract class BaseFormComponent<TEntity, TForm> : BaseComponent where TForm : BaseForm<TEntity>
        where TEntity : class, new()
    {
        protected TForm Form { get; set; }
        protected abstract Task InitializeForm();
        
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            Form = GetService<TForm>();
            Form.SetParent(this);
            await ConfigureFormAsync();
            await InitializeForm();
            MarkAsInitialized();
        }

        protected abstract Task ConfigureFormAsync();
    }
}
