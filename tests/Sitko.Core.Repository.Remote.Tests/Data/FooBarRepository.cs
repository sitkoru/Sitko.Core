using System;
using JetBrains.Annotations;
using Sitko.Core.Repository.Tests.Data;

namespace Sitko.Core.Repository.Remote.Tests.Data;

public class FooBarRepository : BaseRemoteRepository<FooBarModel, Guid>
{
    public FooBarRepository(RemoteRepositoryContext<FooBarModel, Guid> repositoryContext, IRemoteRepositoryTransport repositoryTransport) : base(repositoryContext, repositoryTransport)
    {
    }
}
