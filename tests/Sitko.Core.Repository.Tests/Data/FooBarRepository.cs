using System;
using Sitko.Core.Repository.EntityFrameworkCore;

namespace Sitko.Core.Repository.Tests.Data;

public class FooBarRepository : EFRepository<FooBarModel, Guid, SecondTestDbContext>
{
    public FooBarRepository(EFRepositoryContext<FooBarModel, Guid, SecondTestDbContext> repositoryContext) :
        base(repositoryContext)
    {
    }
}
