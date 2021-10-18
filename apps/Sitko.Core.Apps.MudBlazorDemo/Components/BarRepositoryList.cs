using System;
using System.Threading.Tasks;
using Sitko.Core.Apps.MudBlazorDemo.Data.Entities;
using Sitko.Core.Apps.MudBlazorDemo.Data.Repositories;
using Sitko.Core.Blazor.MudBlazorComponents;

namespace Sitko.Core.Apps.MudBlazorDemo.Components
{
    public class BarRepositoryList : MudRepositoryTable<BarModel, Guid, BarRepository>
    {
        public Task UpdateAsync(BarModel barModel) =>
            ExecuteRepositoryOperation(async repository =>
            {
                barModel.Date = DateTimeOffset.UtcNow;
                var result = await repository.UpdateAsync(barModel);
                return result.IsSuccess;
            });
    }
}
