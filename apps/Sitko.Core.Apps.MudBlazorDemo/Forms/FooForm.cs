using System;
using System.Threading.Tasks;
using Sitko.Core.Apps.MudBlazorDemo.Data.Entities;
using Sitko.Core.Apps.MudBlazorDemo.Data.Repositories;
using Sitko.Core.Blazor.Forms;
using Sitko.Core.Blazor.MudBlazorComponents;

namespace Sitko.Core.Apps.MudBlazorDemo.Forms;

public class FooForm : BaseMudRepositoryForm<FooModel, Guid, FooRepository>
{
    protected override async Task<FormSaveResult> AddAsync(FooModel entity)
    {
        await Task.Delay(2000);
        return await base.AddAsync(entity);
    }
}
