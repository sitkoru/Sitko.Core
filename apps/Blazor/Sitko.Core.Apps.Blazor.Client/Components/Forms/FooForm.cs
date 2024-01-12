using Sitko.Core.Apps.Blazor.Data.Entities;
using Sitko.Core.Blazor.Forms;
using Sitko.Core.Blazor.MudBlazorComponents;
using Sitko.Core.Repository;

namespace Sitko.Core.Apps.Blazor.Client.Components.Forms;

public class FooForm : BaseMudRepositoryForm<FooModel, Guid, IRepository<FooModel, Guid>>
{
    protected override async Task<FormSaveResult> AddAsync(FooModel entity)
    {
        await Task.Delay(2000);
        return await base.AddAsync(entity);
    }
}
