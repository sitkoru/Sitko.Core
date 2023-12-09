using Sitko.Core.Apps.Blazor.Data.Entities;
using Sitko.Core.Repository.Remote;

namespace Sitko.Core.Apps.Blazor.Client.Data.Repositories;

public class FooModelRemoteRepository(RemoteRepositoryContext<FooModel, Guid> repositoryContext)
    : BaseRemoteRepository<FooModel, Guid>(repositoryContext);
