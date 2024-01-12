using Sitko.Core.Apps.Blazor.Data.Entities;
using Sitko.Core.Blazor.MudBlazorComponents;
using Sitko.Core.Repository;

namespace Sitko.Core.Apps.Blazor.Client.Components.Lists
{
    public class BarRepositoryList : MudRepositoryTable<BarModel, Guid, IRepository<BarModel, Guid>>
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
