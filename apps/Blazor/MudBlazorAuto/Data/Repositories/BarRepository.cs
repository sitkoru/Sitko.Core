using MudBlazorAuto.Data.Entities;
using Sitko.Core.Repository.EntityFrameworkCore;

namespace MudBlazorAuto.Data.Repositories
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
