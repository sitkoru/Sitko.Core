using FluentAssertions;
using KellermanSoftware.CompareNetObjects;
using Sitko.Core.Blazor.Forms;
using Sitko.Core.Repository;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.App.Blazor.Tests;

public class FormsTest : BaseTest
{
    public FormsTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Compare()
    {
        var entityA = new SubEntityA();
        var entityB = new SubEntityB();
        var subOtherClassA = new SubOtherClassA();
        var subOtherClassB = new SubOtherClassB();
        var entity = new MainEntity
        {
            SubEntities = new List<SubEntity> { entityA, entityB },
            SubOtherClasses = new List<SubOtherClass> { subOtherClassA, subOtherClassB }
        };
        var form = new TestForm(entity);
        await form.InitAsync();
        form.Entity.Id.Should().Be(entity.Id);
        form.EntityId.Should().Be(entity.Id);
        await form.DetectChangesAsync();
        form.HasChanges.Should().BeFalse();
        entity.StringProperty = "Test";
        await form.DetectChangesAsync();
        form.HasChanges.Should().BeTrue();
        form.Changes.Should().ContainSingle(change => change.Property == nameof(entity.StringProperty));
        entity.StringProperty = "";
        await form.DetectChangesAsync();
        form.HasChanges.Should().BeFalse();
        entity.SubEntities = new List<SubEntity> { entityB, entityA };
        await form.DetectChangesAsync();
        form.HasChanges.Should().BeFalse();
        entity.SubOtherClasses = new List<SubOtherClass> { subOtherClassB, subOtherClassA };
        await form.DetectChangesAsync();
        form.HasChanges.Should().BeFalse();
        entityA.BoolProperty = !entityA.BoolProperty;
        await form.DetectChangesAsync();
        form.HasChanges.Should().BeTrue();
        form.Changes.Should().ContainSingle(change =>
            change.Property ==
            $"{nameof(entity.SubEntities)}[{nameof(entityA.EntityId)}:{entityA.Id}].{nameof(entityA.BoolProperty)}");
        subOtherClassA.BoolProperty = !subOtherClassA.BoolProperty;
        await form.DetectChangesAsync();
        form.HasChanges.Should().BeTrue();
        form.Changes.Should().HaveCount(2);
        form.Changes.Should().ContainSingle(change =>
            change.Property ==
            $"{nameof(entity.SubOtherClasses)}[{nameof(subOtherClassA.Id)}:{subOtherClassA.Id}].{nameof(subOtherClassA.BoolProperty)}");
    }
}

internal sealed class TestForm : BaseRepositoryForm<MainEntity, Guid, MainEntityRepository>
{
    private readonly MainEntity entity;

    public TestForm(MainEntity entity) => this.entity = entity;
    protected override Task<(bool IsNew, MainEntity Entity)> GetEntityAsync() => Task.FromResult((false, entity));

    public Task InitAsync() => InitializeAsync();

    protected override void ConfigureComparer(ComparisonConfig comparisonConfig)
    {
        base.ConfigureComparer(comparisonConfig);
        comparisonConfig.CollectionMatchingSpec.Add(typeof(SubOtherClass), new[] { nameof(SubOtherClass.Id) });
    }
}

internal abstract class MainEntityRepository : BaseRepository<MainEntity, Guid, IRepositoryQuery<MainEntity>>
{
    protected MainEntityRepository(IRepositoryContext<MainEntity, Guid> repositoryContext) : base(repositoryContext)
    {
    }
}

internal sealed class MainEntity : Entity<Guid>
{
    public string StringProperty { get; set; } = "";
    public List<SubEntity> SubEntities { get; set; } = new();
    public List<SubOtherClass> SubOtherClasses { get; set; } = new();
}

internal abstract class SubEntity : Entity<Guid>
{
    public override Guid Id { get; set; } = Guid.NewGuid();

    public int IntProperty { get; set; }
}

internal sealed class SubEntityA : SubEntity
{
    public bool BoolProperty { get; set; }
}

internal sealed class SubEntityB : SubEntity
{
    public Guid GuidProperty { get; set; } = Guid.NewGuid();
}

internal abstract class SubOtherClass
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public int IntProperty { get; set; }
}

internal sealed class SubOtherClassA : SubOtherClass
{
    public bool BoolProperty { get; set; }
}

internal sealed class SubOtherClassB : SubOtherClass
{
    public Guid GuidProperty { get; set; } = Guid.NewGuid();
}
