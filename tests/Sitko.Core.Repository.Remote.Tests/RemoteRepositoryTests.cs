using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
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
    public async Task Add()
    {
        var scope = await GetScopeAsync();

        var repository = scope.GetService<BaseRemoteRepository<TestModel, Guid>>();

        var item = await repository.GetAsync();

        Assert.NotNull(item);
        var oldValue = item!.Status;
        item.Status = TestStatus.Disabled;

        var result = await repository.UpdateAsync(item);
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
        Assert.NotEmpty(result.Changes);
        Assert.NotEmpty(result.Changes.Where(c => c.Name == nameof(item.Status)));
        var change = result.Changes.First(c => c.Name == nameof(item.Status));
        Assert.Equal(oldValue, change.OriginalValue);
        Assert.Equal(item.Status, change.CurrentValue);
        await repository.RefreshAsync(item);
        Assert.Equal(TestStatus.Disabled, item.Status);
    }
}
