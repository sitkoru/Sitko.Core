using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Sitko.Core.Repository.EntityFrameworkCore;

namespace Sitko.Core.Repository.Tests.Data
{
    public class FooRepository : EFRepository<FooModel, Guid, TestDbContext>
    {
        public FooRepository(EFRepositoryContext<FooModel, Guid, TestDbContext> repositoryContext) : base(
            repositoryContext)
        {
        }

        protected override IQueryable<FooModel> AddIncludes(IQueryable<FooModel> query) =>
            base.AddIncludes(query).Include(f => f.BazModels);
    }
}