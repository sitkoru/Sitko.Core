using Microsoft.AspNetCore.Mvc;
using Sitko.Core.Apps.Blazor.Data.Entities;
using Sitko.Core.Repository;
using Sitko.Core.Repository.Remote.Server;

namespace Sitko.Core.Apps.Blazor.Controllers;
[Route("/api/BarModel")]
public class BarModelRepositoryController : BaseRemoteRepositoryController<BarModel, Guid>
{
    public BarModelRepositoryController(IRepository<BarModel, Guid> repository,
        ILogger<BarModelRepositoryController> logger) : base(repository, logger)
    {
    }
}
