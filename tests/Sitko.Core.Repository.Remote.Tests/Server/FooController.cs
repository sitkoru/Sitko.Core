using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sitko.Core.Repository.Remote.Server;
using Sitko.Core.Repository.Tests.Data;

namespace Sitko.Core.Repository.Remote.Tests.Server;

[Route("/api/FooModel")]
public class FooController : BaseRemoteRepositoryController<FooModel, Guid>
{
    public FooController(IRepository<FooModel, Guid> repository,
        ILogger<BaseRemoteRepositoryController<FooModel, Guid>> logger) : base(repository, logger)
    {
    }
}
