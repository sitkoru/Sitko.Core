using System;
using JetBrains.Annotations;
using Sitko.Core.Repository.Tests.Data;

namespace Sitko.Core.Repository.Remote.Tests.Data;

public class BarRepository : BaseRemoteRepository<BarModel, Guid>
{
    protected BarRepository([NotNull] IRepositoryContext<BarModel, Guid> repositoryContext, [NotNull] IRemoteRepositoryTransport repositoryTransport) : base(repositoryContext, repositoryTransport)
    {
    }
}
