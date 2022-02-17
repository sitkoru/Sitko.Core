using System;
using Sitko.Core.Repository.Tests.Data;

namespace Sitko.Core.Repository.EntityFrameworkCore.Tests.Data;

public class TestRepository : EFRepository<TestModel, Guid, TestDbContext>
{
    public TestRepository(EFRepositoryContext<TestModel, Guid, TestDbContext> repositoryContext) : base(
        repositoryContext)
    {
    }
}
