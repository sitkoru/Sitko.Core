using MudBlazorUnited.Data.Entities;
using MudBlazorUnited.Data.Repositories;
using Sitko.Core.Blazor.MudBlazorComponents;

namespace MudBlazorUnited.Components.Lists
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
