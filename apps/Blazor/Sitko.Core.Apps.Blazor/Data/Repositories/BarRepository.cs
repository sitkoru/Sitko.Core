using Sitko.Core.Apps.Blazor.Data.Entities;
using Sitko.Core.Repository.EntityFrameworkCore;

namespace Sitko.Core.Apps.Blazor.Data.Repositories
{
    public class BarRepository : EFRepository<BarModel, Guid, BarContext>
    {
        public BarRepository(EFRepositoryContext<BarModel, Guid, BarContext> repositoryContext) : base(
            repositoryContext)
        {
        }
    }

    public class FooRepository : EFRepository<FooModel, Guid, BarContext>
    {
        public FooRepository(EFRepositoryContext<FooModel, Guid, BarContext> repositoryContext) : base(
            repositoryContext)
        {
        }
    }
}
