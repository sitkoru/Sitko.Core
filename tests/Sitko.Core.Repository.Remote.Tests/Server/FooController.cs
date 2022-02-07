using System;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Sitko.Core.Repository.Remote.Server;
using Sitko.Core.Repository.Tests.Data;

namespace Sitko.Core.Repository.Remote.Tests.Server;

[Route("/api/FooModel")]
public class FooController : BaseRemoteRepositoryController<FooModel, Guid>
{
    public FooController(FooEFRepository repository) : base(repository)
    {
    }
}
