using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.Repository.EntityFrameworkCore.Tests.Data;
using Sitko.Core.Repository.Tests.Data;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Repository.EntityFrameworkCore.Tests;

public class EFDisconnectedTests : BaseTest<EFTestScope>
{
    public EFDisconnectedTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task DisconnectedProperty()
    {
        var scope = await GetScopeAsync();
        BarModel? originalBar;
        using (var scope1 = scope.CreateScope())
        {
            var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
            originalBar = await repository1.GetAsync(q => q.Where(b => b.TestId != null));
            Assert.NotNull(originalBar);
        }

        BarModel? bar;
        using (var scope2 = scope.CreateScope())
        {
            var repository2 = scope2.ServiceProvider.GetRequiredService<BarRepository>();
            bar = await repository2.GetAsync(q => q.Where(b => b.Id == originalBar.Id));
            Assert.NotNull(bar);
        }

        Assert.Null(bar.Baz);
        bar.Baz = "123";

        using (var scope2 = scope.CreateScope())
        {
            var repository2 = scope2.ServiceProvider.GetRequiredService<BarRepository>();
            var updateResult = await repository2.UpdateAsync(bar, originalBar);
            Assert.True(updateResult.IsSuccess);
            updateResult.Changes.Should().ContainSingle();
        }

        using (var finalScope = scope.CreateScope())
        {
            var repository = finalScope.ServiceProvider.GetRequiredService<BarRepository>();
            var updatedBar =
                await repository.GetAsync(q => q.Where(b => b.Id == bar.Id));
            Assert.Equal("123", updatedBar!.Baz);
        }
    }

    [Fact]
    public async Task DisconnectedPropertyNoOriginal()
    {
        var scope = await GetScopeAsync();
        BarModel? bar;
        using (var scope2 = scope.CreateScope())
        {
            var repository2 = scope2.ServiceProvider.GetRequiredService<BarRepository>();
            bar = await repository2.GetAsync(q => q.Where(b => b.TestId != null));
            bar.Should().NotBeNull();
        }

        bar!.Baz.Should().BeNull();
        bar.Baz = "123";

        using (var scope2 = scope.CreateScope())
        {
            var repository2 = scope2.ServiceProvider.GetRequiredService<BarRepository>();
            var updateResult = await repository2.UpdateAsync(bar);
            updateResult.IsSuccess.Should().BeTrue();
            updateResult.Changes.Should().ContainSingle();
        }

        using (var finalScope = scope.CreateScope())
        {
            var repository = finalScope.ServiceProvider.GetRequiredService<BarRepository>();
            var updatedBar =
                await repository.GetAsync(q => q.Where(b => b.Id == bar.Id));
            updatedBar!.Baz.Should().BeEquivalentTo("123");
        }
    }

    [Fact]
    public async Task DisconnectedDelete()
    {
        var scope = await GetScopeAsync();
        BarModel? originalBar;
        using (var scope1 = scope.CreateScope())
        {
            var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
            originalBar = await repository1.GetAsync(q => q.Where(b => b.TestId != null));
            Assert.NotNull(originalBar);
        }

        using (var scope2 = scope.CreateScope())
        {
            var repository2 = scope2.ServiceProvider.GetRequiredService<BarRepository>();
            var updateResult = await repository2.DeleteAsync(originalBar);
            Assert.True(updateResult);
        }

        using (var finalScope = scope.CreateScope())
        {
            var repository = finalScope.ServiceProvider.GetRequiredService<BarRepository>();
            var updatedBar =
                await repository.GetAsync(q => q.Where(b => b.Id == originalBar.Id));
            Assert.Null(updatedBar);
        }
    }

    [Fact]
    public async Task UpdateWithExistingInDbContext()
    {
        var scope = await GetScopeAsync();
        var repository = scope.GetService<BarRepository>();
        var attached = await repository.GetAsync(q => q.Where(b => b.TestId != null));
        attached.Should().NotBeNull();

        var detached = await repository.GetAsync(q => q.Where(b => b.TestId != null).AsNoTracking());
        detached.Should().NotBeNull();
        attached.Should().Be(detached);

        detached!.TestId = Guid.NewGuid();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await repository.UpdateAsync(detached));
        ex.Message.Should()
            .Contain(
                $"The instance of entity type '{nameof(BarModel)}' cannot be tracked because another instance with the key value '{{Id: {attached!.Id}}}' is already being tracked");
    }

    [Fact]
    public async Task UpdateWithExistingInDbContextWithDisabledTracking()
    {
        var scope = await GetScopeAsync();
        var repository = scope.GetService<FooRepository>();
        var attached = await repository.GetAsync();
        attached.Should().NotBeNull();
        var text = Guid.NewGuid().ToString();
        using (repository.DisableTracking())
        {
            var detached = await repository.GetByIdAsync(attached!.Id, q => q.AsNoTracking());
            detached.Should().NotBeNull();
            attached.Id.Should().Be(detached!.Id);

            detached.FooText = text;
            await repository.UpdateAsync(detached);
        }

        await repository.RefreshAsync(attached);
        attached.FooText.Should().Be(text);
    }

    [Fact]
    public async Task UpdateAfterDisablingTracking()
    {
        var scope = await GetScopeAsync();
        var repository = scope.GetService<FooRepository>();
        var attached = await repository.GetAsync();
        attached.Should().NotBeNull();
        var oldText = attached!.FooText;
        var realText = Guid.NewGuid().ToString();
        attached.FooText = realText;
        var tmpText = Guid.NewGuid().ToString();
        using (repository.DisableTracking())
        {
            var detached = await repository.GetByIdAsync(attached.Id, q => q.AsNoTracking());
            detached.Should().NotBeNull();
            attached.Id.Should().Be(detached!.Id);

            detached.FooText = tmpText;
            await repository.UpdateAsync(detached);
        }

        var result = await repository.UpdateAsync(attached);
        result.IsSuccess.Should().BeTrue();
        result.Changes.Length.Should().Be(1);
        result.Changes.First().Name.Should().Be(nameof(FooModel.FooText));
        result.Changes.First().OriginalValue.Should().Be(oldText, "We compare it with value in main dbContext change tracker");
        result.Changes.First().CurrentValue.Should().Be(realText);
        await repository.RefreshAsync(attached);
        attached.FooText.Should().Be(realText);
    }

    [Fact]
    public async Task UpdateAfterDisablingTrackingInBatch()
    {
        var scope = await GetScopeAsync();
        var repository = scope.GetService<FooRepository>();
        await repository.BeginBatchAsync();
        var attached = await repository.GetAsync();
        attached.Should().NotBeNull();
        var realText = Guid.NewGuid().ToString();
        attached!.FooText = realText;
        var tmpText = Guid.NewGuid().ToString();
        using (repository.DisableTracking())
        {
            var detached = await repository.GetByIdAsync(attached.Id, q => q.AsNoTracking());
            detached.Should().NotBeNull();
            attached.Id.Should().Be(detached!.Id);

            detached.FooText = tmpText;
            await repository.UpdateAsync(detached);
        }

        await repository.CommitBatchAsync();
        await repository.RefreshAsync(attached);
        attached.FooText.Should().Be(realText);
    }

    [Fact]
    public async Task UpdateAfterDisablingTrackingInBatchCheckSave()
    {
        var scope = await GetScopeAsync();
        var repository = scope.GetService<FooRepository>();
        await repository.BeginBatchAsync();
        var attached = await repository.GetAsync();
        attached.Should().NotBeNull();
        var tmpText = Guid.NewGuid().ToString();
        using (repository.DisableTracking())
        {
            var detached = await repository.GetByIdAsync(attached!.Id, q => q.AsNoTracking());
            detached.Should().NotBeNull();
            attached.Id.Should().Be(detached!.Id);

            detached.FooText = tmpText;
            await repository.UpdateAsync(detached);
        }

        await repository.CommitBatchAsync();
        await repository.RefreshAsync(attached);
        attached.FooText.Should().Be(tmpText);
    }
}
