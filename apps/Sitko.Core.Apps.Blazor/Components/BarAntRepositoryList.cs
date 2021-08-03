using System;
using System.Threading;
using System.Threading.Tasks;
using Sitko.Core.Apps.Blazor.Data.Entities;
using Sitko.Core.Blazor.AntDesignComponents.Components;

namespace Sitko.Core.Apps.Blazor.Components
{
    using Data;
    using Data.Repositories;
    using Microsoft.EntityFrameworkCore;
    using Repository.EntityFrameworkCore;

    public class
        BarAntRepositoryList : BaseAntRepositoryList<BarModel, Guid, BarRepository>
    {
        public Task UpdateAsync(BarModel barModel) =>
            ExecuteRepositoryOperation(async repository =>
            {
                var result = await repository.UpdateExternalAsync(barModel, model =>
                {
                    model.Date = DateTimeOffset.UtcNow;
                });
                return result.IsSuccess;
            });
    }
}
