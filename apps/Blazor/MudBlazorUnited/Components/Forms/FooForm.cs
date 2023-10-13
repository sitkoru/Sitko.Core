using MudBlazorUnited.Data.Entities;
using MudBlazorUnited.Data.Repositories;
using Sitko.Core.Blazor.Forms;
using Sitko.Core.Blazor.MudBlazorComponents;

namespace MudBlazorUnited.Components.Forms;

public class FooForm : BaseMudRepositoryForm<FooModel, Guid, FooRepository>
{
    protected override async Task<FormSaveResult> AddAsync(FooModel entity)
    {
        await Task.Delay(2000);
        return await base.AddAsync(entity);
    }
}
