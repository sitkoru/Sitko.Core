using Microsoft.AspNetCore.Mvc;
using Sitko.Core.Repository;
using Sitko.Core.Repository.Remote.Server;
using WASMDemo.Shared.Data.Models;

namespace WASMDemo.Server.Controllers;

[Route("/api/TestEntity")]
public class PostController : BaseRemoteRepositoryController<TestEntity, Guid>
{
    public PostController(IRepository<TestEntity, Guid> repository, ILogger<BaseRemoteRepositoryController<TestEntity, Guid>> logger) : base(repository, logger)
    {
    }
}
