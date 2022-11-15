using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sitko.Core.Repository.Remote.Server;
using Sitko.Core.Repository.Tests.Data;

namespace Sitko.Core.Repository.Remote.Tests.Server;

[Route("/api/BarModel")]
public class BarController : BaseRemoteRepositoryController<BarModel, Guid>
{
    public BarController(IRepository<BarModel, Guid> repository,
        ILogger<BaseRemoteRepositoryController<BarModel, Guid>> logger) : base(repository, logger)
    {
    }
}

