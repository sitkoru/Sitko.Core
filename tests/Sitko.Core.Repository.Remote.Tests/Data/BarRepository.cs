using System;
using JetBrains.Annotations;
using Sitko.Core.Repository.Tests.Data;

namespace Sitko.Core.Repository.Remote.Tests.Data;

public class BarRepository : BaseRemoteRepository<BarModel, Guid>
{
    public BarRepository(IRepositoryContext<BarModel, Guid> repositoryContext, IRemoteRepositoryTransport repositoryTransport) : base(repositoryContext, repositoryTransport)
    {
    }
}
