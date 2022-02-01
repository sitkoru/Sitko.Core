using System;
using JetBrains.Annotations;
using Sitko.Core.Repository.Tests.Data;

namespace Sitko.Core.Repository.Remote.Tests.Data;

public class BazRepository : BaseRemoteRepository<BazModel, Guid>
{
    protected BazRepository([NotNull] IRepositoryContext<BazModel, Guid> repositoryContext, [NotNull] IRemoteRepositoryTransport repositoryTransport) : base(repositoryContext, repositoryTransport)
    {
    }
}
