using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Sitko.Core.Repository.Tests.Data;

namespace Sitko.Core.Repository.EntityFrameworkCore.Tests.Data;

public class BarRepository : EFRepository<BarModel, Guid, TestDbContext>
{
    public BarRepository(EFRepositoryContext<BarModel, Guid, TestDbContext> repositoryContext) : base(
        repositoryContext)
    {
    }

    protected override IQueryable<BarModel> AddIncludes(IQueryable<BarModel> query) =>
        base.AddIncludes(query).Include(b => b.BazModels).Include(b => b.Foos).ThenInclude(f => f.BazModels);
}
