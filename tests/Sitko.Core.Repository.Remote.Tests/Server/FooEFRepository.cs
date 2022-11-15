using Sitko.Core.Repository.EntityFrameworkCore;
using Sitko.Core.Repository.Tests.Data;

namespace Sitko.Core.Repository.Remote.Tests.Server;

public class FooEFRepository : EFRepository<FooModel, Guid, TestDbContext>
{
    public FooEFRepository(EFRepositoryContext<FooModel, Guid, TestDbContext> repositoryContext) : base(
        repositoryContext)
    {
    }
}

