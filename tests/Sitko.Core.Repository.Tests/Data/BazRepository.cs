using System;
using Sitko.Core.Repository.EntityFrameworkCore;

namespace Sitko.Core.Repository.Tests.Data
{
    public class BazRepository : EFRepository<BazModel, Guid, TestDbContext>
    {
        public BazRepository(EFRepositoryContext<BazModel, Guid, TestDbContext> repositoryContext) : base(
            repositoryContext)
        {
        }
    }
}