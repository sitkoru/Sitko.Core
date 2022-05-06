using System;
using Sitko.Core.Repository.Tests.Data;

namespace Sitko.Core.Repository.Remote.Tests.Data;

public class BazRepository : BaseRemoteRepository<BazModel, Guid>
{
    public BazRepository(RemoteRepositoryContext<BazModel, Guid> repositoryContext) : base(repositoryContext)
    {
    }
}
