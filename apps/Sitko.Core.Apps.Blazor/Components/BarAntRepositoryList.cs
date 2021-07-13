using System;
using System.Threading;
using System.Threading.Tasks;
using Sitko.Core.Apps.Blazor.Data.Entities;
using Sitko.Core.Blazor.AntDesignComponents.Components;

namespace Sitko.Core.Apps.Blazor.Components
{
    public class
        BarAntRepositoryList : AntRepositoryList<BarModel, Guid>
    {
        public async Task DeleteAsync(BarModel barModel)
        {
            await Repository.DeleteAsync(barModel);
            await RefreshAsync();
        }
    }
}
