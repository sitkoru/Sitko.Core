using Sitko.Core.Repository.EntityFrameworkCore;
using Sitko.Core.Repository.Tests.Data;

namespace Sitko.Core.Repository.Remote.Tests.Server;

public class TestEFRepository : EFRepository<TestModel, Guid, TestDbContext>
{
    public TestEFRepository(EFRepositoryContext<TestModel, Guid, TestDbContext> repositoryContext) : base(
        repositoryContext)
    {
    }
}

