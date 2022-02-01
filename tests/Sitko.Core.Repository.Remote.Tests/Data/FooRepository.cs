using System;
using JetBrains.Annotations;
using Sitko.Core.Repository.Tests.Data;

namespace Sitko.Core.Repository.Remote.Tests.Data;

public class FooRepository : BaseRemoteRepository<FooModel, Guid>
{
    protected FooRepository([NotNull] IRepositoryContext<FooModel, Guid> repositoryContext, [NotNull] IRemoteRepositoryTransport repositoryTransport) : base(repositoryContext, repositoryTransport)
    {
    }
}
