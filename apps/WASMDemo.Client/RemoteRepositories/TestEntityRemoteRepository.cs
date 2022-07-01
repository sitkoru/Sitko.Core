using Sitko.Core.Repository.Remote;
using WASMDemo.Shared.Data.Models;

namespace WASMDemo.Client.RemoteRepositories;

public class TestEntityRemoteRepository : BaseRemoteRepository<TestEntity, Guid>
{
    public TestEntityRemoteRepository(RemoteRepositoryContext<TestEntity, Guid> repositoryContext) : base(repositoryContext)
    {
    }
}
