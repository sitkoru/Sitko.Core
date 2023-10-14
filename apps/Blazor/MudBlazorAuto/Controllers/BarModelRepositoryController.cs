using Microsoft.AspNetCore.Mvc;
using MudBlazorAuto.Data.Entities;
using Sitko.Core.Repository;
using Sitko.Core.Repository.Remote.Server;

namespace MudBlazorAuto.Controllers;
[Route("/api/BarModel")]
public class BarModelRepositoryController : BaseRemoteRepositoryController<BarModel, Guid>
{
    public BarModelRepositoryController(IRepository<BarModel, Guid> repository,
        ILogger<BarModelRepositoryController> logger) : base(repository, logger)
    {
    }
}
