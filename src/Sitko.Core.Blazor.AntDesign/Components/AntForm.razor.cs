using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sitko.Core.App.Blazor.Forms;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public partial class AntForm<TEntity, TForm> where TForm : BaseForm<TEntity>
        where TEntity : class, new()
    {
        [Parameter] public TEntity Entity { get; set; }
        protected override async Task InitializeForm()
        {
            await Form.InitializeAsync(Entity);
        }
    }
}
