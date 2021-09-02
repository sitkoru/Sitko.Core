using System;
using Sitko.Core.Repository.EntityFrameworkCore;

namespace Sitko.Core.Repository.Tests.Data
{
    public class TestRepository : EFRepository<TestModel, Guid, TestDbContext>
    {
        public TestRepository(EFRepositoryContext<TestModel, Guid, TestDbContext> repositoryContext) : base(
            repositoryContext)
        {
        }
    }
}