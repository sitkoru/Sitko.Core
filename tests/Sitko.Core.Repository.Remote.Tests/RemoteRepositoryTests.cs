using System;
using System.Linq;
using System.Threading.Tasks;
using Sitko.Core.Repository.Remote.Tests.Data;
using Sitko.Core.Repository.Tests;
using Sitko.Core.Repository.Tests.Data;
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

    [Fact]
    public async Task ThenInclude()
    {
        var scope = await GetScopeAsync();

        var repository = scope.GetService<IRepository<TestModel, Guid>>();
        Assert.NotNull(repository);

        var item = await repository.GetAsync(query => query.Where(model => model.Bars.Any())
            .Include(testModel => testModel.Bars).ThenInclude(barModel => barModel.Foos));
        Assert.NotNull(item);
        Assert.NotNull(item!.Bars);
        Assert.NotEmpty(item.Bars);
        Assert.Single(item.Bars);
        var bar = item.Bars.First();
        Assert.Equal(item.Id, bar.TestId);
        Assert.NotEmpty(bar.Foos);
    }
}
