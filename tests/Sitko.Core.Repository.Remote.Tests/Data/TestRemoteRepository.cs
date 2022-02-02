using System;
using JetBrains.Annotations;
using Sitko.Core.Repository.Tests.Data;

namespace Sitko.Core.Repository.Remote.Tests.Data;

public class TestRemoteRepository : BaseRemoteRepository<TestModel, Guid>
{
    public TestRemoteRepository([NotNull] RemoteRepositoryContext<TestModel, Guid> repositoryContext, [NotNull] IRemoteRepositoryTransport repositoryTransport) : base(repositoryContext, repositoryTransport)
    {
    }
}
