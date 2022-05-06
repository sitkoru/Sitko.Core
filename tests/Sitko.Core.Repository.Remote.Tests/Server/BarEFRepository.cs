using System;
using JetBrains.Annotations;
using Sitko.Core.Repository.EntityFrameworkCore;
using Sitko.Core.Repository.Tests.Data;

namespace Sitko.Core.Repository.Remote.Tests.Server;

public class BarEFRepository : EFRepository<BarModel, Guid, TestDbContext>
{
    public BarEFRepository([NotNull] EFRepositoryContext<BarModel, Guid, TestDbContext> repositoryContext) : base(
        repositoryContext)
    {
    }
}
