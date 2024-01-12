using Sitko.Core.Apps.Blazor.Data.Entities;
using Sitko.Core.Repository.Remote;

namespace Sitko.Core.Apps.Blazor.Client.Data.Repositories;

public class BarModelRemoteRepository(RemoteRepositoryContext<BarModel, Guid> repositoryContext)
    : BaseRemoteRepository<BarModel, Guid>(repositoryContext);
