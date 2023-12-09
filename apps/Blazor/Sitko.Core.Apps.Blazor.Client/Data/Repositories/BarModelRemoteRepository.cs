using Sitko.Core.Apps.Blazor.Data.Entities;
using Sitko.Core.Repository.Remote;

namespace Sitko.Core.Apps.Blazor.Client.Data.Repositories;

public class BarModelRemoteRepository : BaseRemoteRepository<BarModel, Guid>
{
    public BarModelRemoteRepository(RemoteRepositoryContext<BarModel, Guid> repositoryContext) : base(repositoryContext)
    {
    }
}
