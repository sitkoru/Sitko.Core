using System;
using JetBrains.Annotations;
using Sitko.Core.Repository.Tests.Data;

namespace Sitko.Core.Repository.Remote.Tests.Data;

public class FooBarRepository : BaseRemoteRepository<FooBarModel, Guid>
{
    protected FooBarRepository([NotNull] IRepositoryContext<FooBarModel, Guid> repositoryContext, [NotNull] IRemoteRepositoryTransport repositoryTransport) : base(repositoryContext, repositoryTransport)
    {
    }
}
