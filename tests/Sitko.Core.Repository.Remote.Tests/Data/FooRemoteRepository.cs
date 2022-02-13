using System;
using JetBrains.Annotations;
using Sitko.Core.Repository.Tests.Data;

namespace Sitko.Core.Repository.Remote.Tests.Data;

public class FooRemoteRepository : BaseRemoteRepository<FooModel, Guid>
{
    public FooRemoteRepository(RemoteRepositoryContext<FooModel, Guid> repositoryContext) : base(repositoryContext)
    {
    }
}
