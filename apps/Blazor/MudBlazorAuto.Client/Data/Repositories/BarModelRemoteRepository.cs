using MudBlazorAuto.Data.Entities;
using Sitko.Core.Repository.Remote;

namespace MudBlazorAuto.Client.Data.Repositories;

public class BarModelRemoteRepository : BaseRemoteRepository<BarModel, Guid>
{
    public BarModelRemoteRepository(RemoteRepositoryContext<BarModel, Guid> repositoryContext) : base(repositoryContext)
    {
    }
}
