using System;
using JetBrains.Annotations;
using Sitko.Core.Repository.Tests.Data;

namespace Sitko.Core.Repository.Remote.Tests.Data;

public class FooRepository : BaseRemoteRepository<FooModel, Guid>
{
    public FooRepository(RemoteRepositoryContext<FooModel, Guid> repositoryContext, IRemoteRepositoryTransport repositoryTransport) : base(repositoryContext, repositoryTransport)
    {
    }
}
