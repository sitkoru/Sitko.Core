using Sitko.Core.Apps.Blazor.Data.Entities;
using Sitko.Core.Apps.Blazor.Data.Repositories;
using Sitko.Core.Blazor.Forms;
using Sitko.Core.Blazor.MudBlazorComponents;

namespace Sitko.Core.Apps.Blazor.Components.Forms;

public class FooForm : BaseMudRepositoryForm<FooModel, Guid, FooRepository>
{
    protected override async Task<FormSaveResult> AddAsync(FooModel entity)
    {
        await Task.Delay(2000);
        return await base.AddAsync(entity);
    }
}
