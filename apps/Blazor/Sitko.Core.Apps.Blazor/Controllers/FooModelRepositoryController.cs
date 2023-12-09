using Microsoft.AspNetCore.Mvc;
using Sitko.Core.Apps.Blazor.Data.Entities;
using Sitko.Core.Repository;
using Sitko.Core.Repository.Remote.Server;

namespace Sitko.Core.Apps.Blazor.Controllers;

[Route("/api/FooModel")]
public class FooModelRepositoryController(
    IRepository<FooModel, Guid> repository,
    ILogger<FooModelRepositoryController> logger)
    : BaseRemoteRepositoryController<FooModel, Guid>(repository, logger);
