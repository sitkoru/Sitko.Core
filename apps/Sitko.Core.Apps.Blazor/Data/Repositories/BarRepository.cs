using System;
using Microsoft.Extensions.Options;
using Sitko.Core.Apps.Blazor.Data.Entities;
using Sitko.Core.Repository.EntityFrameworkCore;

namespace Sitko.Core.Apps.Blazor.Data.Repositories
{
    public class BarRepository : EFRepository<BarModel, Guid, BarContext>
    {
        public BarRepository(IOptionsMonitor<EfRepositoriesModuleOptions> optionsMonitor,
            EFRepositoryContext<BarModel, Guid, BarContext> repositoryContext) : base(optionsMonitor,
            repositoryContext)
        {
        }
    }
}
