using System;
using Sitko.Core.Repository.Tests.Data;

namespace Sitko.Core.Repository.EntityFrameworkCore.Tests.Data;

public class FooBarRepository : EFRepository<FooBarModel, Guid, SecondTestDbContext>
{
    public FooBarRepository(EFRepositoryContext<FooBarModel, Guid, SecondTestDbContext> repositoryContext) :
        base(repositoryContext)
    {
    }
}
