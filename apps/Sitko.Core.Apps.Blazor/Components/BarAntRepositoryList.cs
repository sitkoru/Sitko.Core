using System;
using System.Threading.Tasks;
using Sitko.Core.Apps.Blazor.Data.Entities;

namespace Sitko.Core.Apps.Blazor.Components
{
    public class
        BarAntRepositoryList : Sitko.Core.Blazor.AntDesignComponents.Components.AntRepositoryList<BarModel, Guid>
    {
        public async Task DeleteAsync(BarModel barModel)
        {
            await Repository.DeleteAsync(barModel);
            Refresh();
        }
    }
}
