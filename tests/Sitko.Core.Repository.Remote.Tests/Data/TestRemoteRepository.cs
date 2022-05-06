using System;
using Sitko.Core.Repository.Tests.Data;

namespace Sitko.Core.Repository.Remote.Tests.Data;

public class TestRemoteRepository : BaseRemoteRepository<TestModel, Guid>
{
    public TestRemoteRepository(RemoteRepositoryContext<TestModel, Guid> repositoryContext) : base(repositoryContext)
    {
    }
}
