using System;
using JetBrains.Annotations;
using Sitko.Core.Repository.Tests.Data;

namespace Sitko.Core.Repository.Remote.Tests.Data;

public class BarRemoteRepository : BaseRemoteRepository<BarModel, Guid>
{
    public BarRemoteRepository(RemoteRepositoryContext<BarModel, Guid> repositoryContext) : base(repositoryContext)
    {
    }
}
