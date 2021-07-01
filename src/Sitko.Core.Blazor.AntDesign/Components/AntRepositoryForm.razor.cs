using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sitko.Core.App.Blazor.Forms;
using Sitko.Core.Repository;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public partial class AntRepositoryForm<TEntity, TEntityPk, TForm>
        where TForm : BaseRepositoryForm<TEntity, TEntityPk>
        where TEntity : class, IEntity<TEntityPk>, new()
    {
        [Parameter] public TEntityPk? EntityId { get; set; }

        protected override async Task InitializeForm(TForm form)
        {
            await form.InitializeAsync(EntityId);
        }
    }
}
