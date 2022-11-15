using Sitko.Core.Repository.Tests.Data;

namespace Sitko.Core.Repository.EntityFrameworkCore.Tests.Data;

public class BazRepository : EFRepository<BazModel, Guid, TestDbContext>
{
    public BazRepository(EFRepositoryContext<BazModel, Guid, TestDbContext> repositoryContext) : base(
        repositoryContext)
    {
    }
}

