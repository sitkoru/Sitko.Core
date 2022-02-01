using System;
using JetBrains.Annotations;
using Sitko.Core.Repository.Tests.Data;

namespace Sitko.Core.Repository.Remote.Tests.Data;

public class BazRepository : BaseRemoteRepository<BazModel, Guid>
{
    public BazRepository(IRepositoryContext<BazModel, Guid> repositoryContext, IRemoteRepositoryTransport repositoryTransport) : base(repositoryContext, repositoryTransport)
    {
    }
}
