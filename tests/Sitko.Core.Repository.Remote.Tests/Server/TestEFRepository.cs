using System;
using JetBrains.Annotations;
using Sitko.Core.Repository.EntityFrameworkCore;
using Sitko.Core.Repository.Tests.Data;

namespace Sitko.Core.Repository.Remote.Tests.Data;

public class TestEFRepository : EFRepository<TestModel, Guid, TestDbContext>
{
    public TestEFRepository([NotNull] EFRepositoryContext<TestModel, Guid, TestDbContext> repositoryContext) : base(repositoryContext)
    {
    }
}
