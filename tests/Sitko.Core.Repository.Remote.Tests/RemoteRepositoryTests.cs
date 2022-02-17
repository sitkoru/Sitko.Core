using System.Threading.Tasks;
using Sitko.Core.Repository.Remote.Tests.Data;
using Sitko.Core.Repository.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Repository.Remote.Tests;

public class RemoteRepositoryTests : BasicRepositoryTests<RemoteRepositoryTestScope>
{
    public RemoteRepositoryTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task GetAll()
    {
        var scope = await GetScopeAsync();
        var repo = scope.GetService<TestRemoteRepository>();
        var result = await repo.GetAllAsync();

        Assert.NotNull(result.items);
    }
}
