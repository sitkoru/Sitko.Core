using System;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Sitko.Core.Repository.Remote.Server;
using Sitko.Core.Repository.Remote.Tests.Data;
using Sitko.Core.Repository.Tests.Data;

namespace Sitko.Core.Repository.Remote.Tests.Server;

[Route("/test")]
public class RemoteController : BaseRemoteRepositoryController<TestModel,Guid>
{
    public RemoteController(TestEFRepository repository) : base(repository)
    {
    }
}
