using System;
using Microsoft.AspNetCore.Mvc;
using Sitko.Core.Repository.Remote.Server;
using Sitko.Core.Repository.Remote.Tests.Data;
using Sitko.Core.Repository.Tests.Data;

namespace Sitko.Core.Repository.Remote.Tests.Server;

[Route("/api/TestModel")]
public class TestController : BaseRemoteRepositoryController<TestModel, Guid>
{
    public TestController(TestEFRepository repository) : base(repository)
    {
    }
}
