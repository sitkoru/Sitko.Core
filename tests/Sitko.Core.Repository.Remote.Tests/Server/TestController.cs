using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sitko.Core.Repository.Remote.Server;
using Sitko.Core.Repository.Tests.Data;

namespace Sitko.Core.Repository.Remote.Tests.Server;

[Route("/api/TestModel")]
public class TestController : BaseRemoteRepositoryController<TestModel, Guid>
{
    public TestController(IRepository<TestModel, Guid> repository,
        ILogger<BaseRemoteRepositoryController<TestModel, Guid>> logger) : base(repository, logger)
    {
    }
}

