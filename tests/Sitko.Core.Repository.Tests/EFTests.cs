using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.UserSecrets;
using Sitko.Core.Repository.EntityFrameworkCore;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Extensions.DependencyInjection;

[assembly: UserSecretsId("test")]

namespace Sitko.Core.Repository.Tests
{
    public class EFTests : BaseTest<EFTestScope>
    {
        public EFTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task Update()
        {
            var scope = await GetScopeAsync();

            var repository = scope.GetService<IRepository<TestModel, Guid>>();

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
        }

        [Fact]
        public async Task Validation()
        {
            var scope = await GetScopeAsync();
            var validator = scope.GetServices<IValidator>();
            Assert.NotEmpty(validator);

            var typedValidator = scope.GetServices<IValidator<TestModel>>();
            Assert.NotEmpty(typedValidator);

            var repository = scope.GetService<IRepository<TestModel, Guid>>();

            var item = await repository.GetAsync();

            Assert.NotNull(item);
            item!.Status = TestStatus.Error;

            var result = await repository.UpdateAsync(item);
            Assert.False(result.IsSuccess);
            Assert.Single(result.Errors);
            var error = result.Errors.First();
            Assert.Equal(nameof(TestModel.Status), error.PropertyName);
            Assert.Equal("Status can't be error", error.ErrorMessage);
        }

        [Fact]
        public async Task Refresh()
        {
            var scope = await GetScopeAsync();

            var repository = scope.GetService<IRepository<TestModel, Guid>>();

            var item = await repository.GetAsync();

            Assert.NotNull(item);
            var oldValue = item!.Status;
            item.Status = TestStatus.Disabled;

            Assert.NotEqual(oldValue, item.Status);
            await repository.RefreshAsync(item);
            Assert.Equal(oldValue, item.Status);
        }

        [Fact]
        public async Task JsonConditions()
        {
            var json = "[{\"conditions\":[{\"property\":\"fooId\",\"operator\":1,\"value\":1}]}]";

            var scope = await GetScopeAsync();

            var repository = scope.GetService<IRepository<TestModel, Guid>>();

            Assert.NotNull(repository);

            var items = await repository.GetAllAsync(q => q.WhereByString(json));

            Assert.NotEqual(0, items.itemsCount);
            Assert.Single(items.items);
        }

        [Fact]
        public async Task IncludeSingle()
        {
            var scope = await GetScopeAsync();

            var repository = scope.GetService<BarRepository>();

            Assert.NotNull(repository);

            var item = await repository.GetAsync(query => query.Where(e => e.TestId != null).Include(e => e.Test));
            Assert.NotNull(item);
            Assert.NotNull(item!.Test);
            Assert.Equal(1, item.Test!.FooId);
        }

        [Fact]
        public async Task IncludeCollection()
        {
            var scope = await GetScopeAsync();

            var repository = scope.GetService<IRepository<TestModel, Guid>>();

            Assert.NotNull(repository);

            var item = await repository.GetAsync(query => query.Include(e => e.Bars));
            Assert.NotNull(item);
            Assert.NotNull(item!.Bars);
            Assert.NotEmpty(item.Bars);
            Assert.Single(item.Bars);
            Assert.Equal(item.Id, item.Bars.First().TestId);
        }

        [Fact]
        public async Task ThenInclude()
        {
            var scope = await GetScopeAsync();

            var repository = scope.GetService<IRepository<TestModel, Guid>>();
            Assert.NotNull(repository);

            var item = await repository.GetAsync(query => query
                .Include(testModel => testModel.Bars).ThenInclude(barModel => barModel.Foos));
            Assert.NotNull(item);
            Assert.NotNull(item!.Bars);
            Assert.NotEmpty(item.Bars);
            Assert.Single(item.Bars);
            var bar = item.Bars.First();
            Assert.Equal(item.Id, bar.TestId);
            Assert.NotEmpty(bar.Foos);
            var foo = bar.Foos.First();
            Assert.NotNull(foo.Bar);
            Assert.NotNull(foo.Bar!.Test);
        }

        [Fact]
        public async Task ThreadSafe()
        {
            var scope = await GetScopeAsync();

            var testRepository = scope.GetService<IRepository<TestModel, Guid>>();
            var barRepository = scope.GetService<BarRepository>();

            var tasks = new List<Task> { testRepository.GetAllAsync(), barRepository.GetAllAsync() };

            await Task.WhenAll(tasks);

            foreach (var task in tasks)
            {
                Assert.True(task.IsCompletedSuccessfully);
            }
        }

        [Fact]
        public async Task Sum()
        {
            var scope = await GetScopeAsync();
            var repository = scope.GetService<IRepository<TestModel, Guid>>();
            var sum = await repository.SumAsync(t => t.FooId);
            var all = await repository.GetAllAsync();
            var allSum = all.items.Sum(t => t.FooId);
            Assert.Equal(allSum, sum);
        }

        [Fact]
        public async Task DisconnectedProperty()
        {
            var scope = await GetScopeAsync();
            BarModel? originalBar;
            BarModel snapshot;
            using (var scope1 = scope.CreateScope())
            {
                var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
                originalBar = await repository1.GetAsync(q => q.Where(b => b.Baz == null));
                Assert.NotNull(originalBar);
                snapshot = repository1.CreateSnapshot(originalBar!);
            }

            Assert.Null(originalBar!.Baz);
            originalBar.Baz = "123";

            using (var scope2 = scope.CreateScope())
            {
                var repository2 = scope2.ServiceProvider.GetRequiredService<BarRepository>();
                var updateResult = await repository2.UpdateExternalAsync(originalBar, snapshot);
                Assert.True(updateResult.IsSuccess);
                Assert.Single(updateResult.Changes);
            }

            using (var finalScope = scope.CreateScope())
            {
                var repository = finalScope.ServiceProvider.GetRequiredService<BarRepository>();
                var updatedBar =
                    await repository.GetAsync(q => q.Where(b => b.Id == originalBar.Id));
                Assert.Equal("123", updatedBar!.Baz);
            }
        }

        [Fact]
        public async Task DisconnectedOneToOne()
        {
            var scope = await GetScopeAsync();
            BarModel? originalBar;
            BarModel snapshot;
            using (var scope1 = scope.CreateScope())
            {
                var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
                originalBar = await repository1.GetAsync(q => q.Where(b => b.TestId == null));
                Assert.NotNull(originalBar);
                snapshot = repository1.CreateSnapshot(originalBar!);
            }

            Assert.Null(originalBar!.Test);
            Assert.Equal(default, originalBar.TestId);

            AddOrUpdateOperationResult<TestModel, Guid> newTestResult;
            using (var scope2 = scope.CreateScope())
            {
                var repository2 = scope2.ServiceProvider.GetRequiredService<IRepository<TestModel, Guid>>();
                newTestResult = await repository2.AddAsync(await repository2.NewAsync());
            }

            Assert.True(newTestResult.IsSuccess);

            originalBar.Test = newTestResult.Entity;


            using (var scope3 = scope.CreateScope())
            {
                var repository3 = scope3.ServiceProvider.GetRequiredService<BarRepository>();
                var updateResult = await repository3.UpdateExternalAsync(originalBar, snapshot);
                Assert.True(updateResult.IsSuccess);
                Assert.Equal(2, updateResult.Changes.Length);
            }

            using (var finalScope = scope.CreateScope())
            {
                var repository = finalScope.ServiceProvider.GetRequiredService<BarRepository>();
                var updatedBar =
                    await repository.GetAsync(q => q.Where(b => b.Id == originalBar.Id));
                Assert.Equal(newTestResult.Entity.Id, updatedBar!.TestId);
            }
        }

        [Fact]
        public async Task DisconnectedOneToOneUpdateEntity()
        {
            var scope = await GetScopeAsync();
            BarModel? originalBar;
            BarModel snapshot;
            using (var scope1 = scope.CreateScope())
            {
                var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
                originalBar = await repository1.GetAsync(q => q.Where(b => b.TestId != null).Include(b => b.Test));
                Assert.NotNull(originalBar);
                snapshot = repository1.CreateSnapshot(originalBar!);
            }

            Assert.NotNull(originalBar!.Test);
            originalBar.Test!.FooId = 10;

            using (var scope3 = scope.CreateScope())
            {
                var repository3 = scope3.ServiceProvider.GetRequiredService<BarRepository>();
                var updateResult = await repository3.UpdateExternalAsync(originalBar, snapshot);
                Assert.True(updateResult.IsSuccess);
                Assert.Single(updateResult.Changes);
                await repository3.RefreshAsync(originalBar);
            }

            using (var finalScope = scope.CreateScope())
            {
                var repository = finalScope.ServiceProvider.GetRequiredService<BarRepository>();
                var updatedBar =
                    await repository.GetAsync(q => q.Where(b => b.Id == originalBar.Id).Include(b => b.Test));
                Assert.Equal(10, updatedBar!.Test!.FooId);
            }
        }


        [Fact]
        public async Task DisconnectedOneToOneDeleteEntity()
        {
            var scope = await GetScopeAsync();
            BarModel? originalBar;
            BarModel snapshot;
            using (var scope1 = scope.CreateScope())
            {
                var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
                originalBar = await repository1.GetAsync(q => q.Where(b => b.TestId != null).Include(b => b.Test));
                Assert.NotNull(originalBar);
                snapshot = repository1.CreateSnapshot(originalBar!);
            }

            Assert.NotNull(originalBar!.Test);
            originalBar.Test = null;

            using (var scope3 = scope.CreateScope())
            {
                var repository3 = scope3.ServiceProvider.GetRequiredService<BarRepository>();
                var updateResult = await repository3.UpdateExternalAsync(originalBar, snapshot);
                Assert.True(updateResult.IsSuccess);
                Assert.Equal(2, updateResult.Changes.Length);
            }

            using (var finalScope = scope.CreateScope())
            {
                var repository = finalScope.ServiceProvider.GetRequiredService<BarRepository>();
                var updatedBar =
                    await repository.GetAsync(q => q.Where(b => b.Id == originalBar.Id).Include(b => b.Foos));
                Assert.Null(updatedBar!.Test);
                Assert.Null(updatedBar.TestId);
            }
        }

        [Fact]
        public async Task DisconnectedOneToMany()
        {
            var scope = await GetScopeAsync();
            BarModel? originalBar;
            BarModel snapshot;
            using (var scope1 = scope.CreateScope())
            {
                var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
                originalBar = await repository1.GetAsync(q => q.Where(b => !b.Foos.Any()));
                Assert.NotNull(originalBar);
                snapshot = repository1.CreateSnapshot(originalBar!);
            }

            Assert.NotNull(originalBar);
            Assert.Empty(originalBar!.Foos);

            AddOrUpdateOperationResult<FooModel, Guid> newTestResult;
            using (var scope2 = scope.CreateScope())
            {
                var repository2 = scope2.ServiceProvider.GetRequiredService<FooRepository>();
                newTestResult = await repository2.AddAsync(await repository2.NewAsync());
            }

            Assert.True(newTestResult.IsSuccess);

            originalBar.Foos.Add(newTestResult.Entity);

            using (var scope3 = scope.CreateScope())
            {
                var repository3 = scope3.ServiceProvider.GetRequiredService<BarRepository>();
                var updateResult = await repository3.UpdateExternalAsync(originalBar, snapshot);
                Assert.True(updateResult.IsSuccess);
                Assert.Single(updateResult.Changes);
            }

            using (var finalScope = scope.CreateScope())
            {
                var repository = finalScope.ServiceProvider.GetRequiredService<BarRepository>();
                var updatedBar =
                    await repository.GetAsync(q => q.Where(b => b.Id == originalBar.Id).Include(b => b.Foos));
                Assert.NotEmpty(updatedBar!.Foos);
            }
        }

        [Fact]
        public async Task DisconnectedOneToManyNewEntity()
        {
            var scope = await GetScopeAsync();
            BarModel? originalBar;
            BarModel snapshot;
            using (var scope1 = scope.CreateScope())
            {
                var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
                originalBar = await repository1.GetAsync(q => q.Where(b => !b.Foos.Any()));
                Assert.NotNull(originalBar);
                snapshot = repository1.CreateSnapshot(originalBar!);
            }

            Assert.Empty(originalBar!.Foos);

            var foo = new FooModel();

            originalBar.Foos.Add(foo);

            using (var scope3 = scope.CreateScope())
            {
                var repository3 = scope3.ServiceProvider.GetRequiredService<BarRepository>();
                var updateResult = await repository3.UpdateExternalAsync(originalBar, snapshot);
                Assert.True(updateResult.IsSuccess);
                Assert.Single(updateResult.Changes);
            }

            using (var finalScope = scope.CreateScope())
            {
                var repository = finalScope.ServiceProvider.GetRequiredService<BarRepository>();
                var updatedBar =
                    await repository.GetAsync(q => q.Where(b => b.Id == originalBar.Id).Include(b => b.Foos));
                Assert.NotEmpty(updatedBar!.Foos);
            }
        }

        [Fact]
        public async Task DisconnectedOneToManyUpdateEntity()
        {
            var scope = await GetScopeAsync();
            BarModel? originalBar;
            BarModel snapshot;
            using (var scope1 = scope.CreateScope())
            {
                var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
                originalBar = await repository1.GetAsync(q => q.Where(b => b.Foos.Any()).Include(b => b.Foos));
                Assert.NotNull(originalBar);
                snapshot = repository1.CreateSnapshot(originalBar!);
            }

            Assert.NotEmpty(originalBar!.Foos);
            var foo = originalBar.Foos.OrderBy(_ => Guid.NewGuid()).First(); // change random
            var newText = Guid.NewGuid().ToString();
            foo.FooText = newText;

            using (var scope3 = scope.CreateScope())
            {
                var repository3 = scope3.ServiceProvider.GetRequiredService<BarRepository>();
                var updateResult = await repository3.UpdateExternalAsync(originalBar, snapshot);
                Assert.True(updateResult.IsSuccess);
                Assert.Single(updateResult.Changes);
            }

            using (var finalScope = scope.CreateScope())
            {
                var repository = finalScope.ServiceProvider.GetRequiredService<BarRepository>();
                var updatedBar =
                    await repository.GetAsync(q => q.Where(b => b.Id == originalBar.Id).Include(b => b.Foos));
                Assert.Equal(newText, updatedBar!.Foos.First(f => f.Id == foo.Id).FooText);
            }
        }

        [Fact]
        public async Task DisconnectedOneToManyMultipleEntity()
        {
            var scope = await GetScopeAsync();
            BarModel? originalBar;
            BarModel snapshot;
            using (var scope1 = scope.CreateScope())
            {
                var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
                originalBar = await repository1.GetAsync(q => q.Where(b => b.Foos.Any()).Include(b => b.Foos));
                Assert.NotNull(originalBar);
                snapshot = repository1.CreateSnapshot(originalBar!);
            }

            Assert.NotEmpty(originalBar!.Foos);
            var count = originalBar.Foos.Count;
            var foo1 = new FooModel();
            var foo2 = new FooModel();
            originalBar.Foos.Remove(originalBar.Foos.Last());
            originalBar.Foos.Add(foo1);
            originalBar.Foos.Add(foo2);

            using (var scope3 = scope.CreateScope())
            {
                var repository3 = scope3.ServiceProvider.GetRequiredService<BarRepository>();
                var updateResult = await repository3.UpdateExternalAsync(originalBar, snapshot);
                Assert.True(updateResult.IsSuccess);
                Assert.Single(updateResult.Changes);
            }

            using (var finalScope = scope.CreateScope())
            {
                var repository = finalScope.ServiceProvider.GetRequiredService<BarRepository>();
                var updatedBar =
                    await repository.GetAsync(q => q.Where(b => b.Id == originalBar.Id).Include(b => b.Foos));
                Assert.NotEmpty(updatedBar!.Foos);
                Assert.Equal(count + 1, updatedBar.Foos.Count);
            }
        }

        [Fact]
        public async Task DisconnectedOneToManyDeleteEntity()
        {
            var scope = await GetScopeAsync();
            BarModel? originalBar;
            BarModel snapshot;
            using (var scope1 = scope.CreateScope())
            {
                var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
                originalBar = await repository1.GetAsync(q => q.Where(b => b.Foos.Any()).Include(b => b.Foos));
                Assert.NotNull(originalBar);
                snapshot = repository1.CreateSnapshot(originalBar!);
            }

            Assert.NotNull(originalBar);
            Assert.NotEmpty(originalBar!.Foos);
            var count = originalBar.Foos.Count;
            Assert.Equal(4, count);

            originalBar.Foos.Remove(originalBar.Foos.OrderBy(_ => Guid.NewGuid()).First()); // delete random
            using (var scope3 = scope.CreateScope())
            {
                var repository3 = scope3.ServiceProvider.GetRequiredService<BarRepository>();
                var updateResult = await repository3.UpdateExternalAsync(originalBar, snapshot);
                Assert.True(updateResult.IsSuccess);
                Assert.Single(updateResult.Changes);
            }

            using (var finalScope = scope.CreateScope())
            {
                var repository = finalScope.ServiceProvider.GetRequiredService<BarRepository>();
                var updatedBar =
                    await repository.GetAsync(q => q.Where(b => b.Id == originalBar.Id).Include(b => b.Foos));
                Assert.Equal(count - 1, updatedBar!.Foos.Count);
            }
        }
    }

    public class TestDbContext : DbContext
    {
        public DbSet<TestModel> TestModels => Set<TestModel>();

        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            var testModels = new List<TestModel>
            {
                new() { Id = Guid.NewGuid(), FooId = 1 },
                new() { Id = Guid.NewGuid(), FooId = 2 },
                new() { Id = Guid.NewGuid(), FooId = 3 },
                new() { Id = Guid.NewGuid(), FooId = 4 },
                new() { Id = Guid.NewGuid(), FooId = 5 },
                new() { Id = Guid.NewGuid(), FooId = 5 }
            };
            modelBuilder.Entity<TestModel>().HasData(testModels);
            var barModels = new List<BarModel>()
            {
                new() { Id = Guid.NewGuid(), TestId = testModels.First().Id }, new() { Id = Guid.NewGuid() }
            };
            modelBuilder.Entity<BarModel>().HasData(barModels);
            modelBuilder.Entity<FooModel>()
                .HasData(new FooModel { Id = Guid.NewGuid(), BarId = barModels[0].Id, FooText = "123" },
                    new FooModel { Id = Guid.NewGuid(), BarId = barModels[0].Id, FooText = "456" },
                    new FooModel { Id = Guid.NewGuid(), BarId = barModels[0].Id, FooText = "789" },
                    new FooModel { Id = Guid.NewGuid(), BarId = barModels[0].Id, FooText = "012" });
        }
    }

    public class TestModel : Entity<Guid>
    {
        public int FooId { get; set; }

        public TestStatus Status { get; set; } = TestStatus.Enabled;

        [InverseProperty(nameof(BarModel.Test))]
        public List<BarModel> Bars { get; set; } = new();
    }

    public class TestModelValidator : AbstractValidator<TestModel>
    {
        public TestModelValidator() =>
            RuleFor(m => m.Status).NotEqual(TestStatus.Error).WithMessage("Status can't be error");
    }

    public enum TestStatus
    {
        Enabled, Disabled, Error
    }

    public class BarModel : Entity<Guid>
    {
        public Guid? TestId { get; set; }
        [ForeignKey(nameof(TestId))] public TestModel? Test { get; set; }

        [InverseProperty(nameof(FooModel.Bar))]
        public List<FooModel> Foos { get; set; } = new();

        public string? Baz { get; set; }
    }

    public class FooModel : Entity<Guid>
    {
        public string? FooText { get; set; }
        public Guid? BarId { get; set; }
        [ForeignKey(nameof(BarId))] public BarModel? Bar { get; set; } = null!;
    }

    public class TestRepository : EFRepository<TestModel, Guid, TestDbContext>
    {
        public TestRepository(EFRepositoryContext<TestModel, Guid, TestDbContext> repositoryContext) : base(
            repositoryContext)
        {
        }
    }

    public class BarRepository : EFRepository<BarModel, Guid, TestDbContext>
    {
        public BarRepository(EFRepositoryContext<BarModel, Guid, TestDbContext> repositoryContext) : base(
            repositoryContext)
        {
        }
    }

    public class FooRepository : EFRepository<FooModel, Guid, TestDbContext>
    {
        public FooRepository(EFRepositoryContext<FooModel, Guid, TestDbContext> repositoryContext) : base(
            repositoryContext)
        {
        }
    }

    public abstract class BaseEFTestScope : DbBaseTestScope<TestDbContext>
    {
    }

    public class EFTestScope : BaseEFTestScope
    {
        protected override TestApplication ConfigureApplication(TestApplication application, string name)
        {
            application.AddEFRepositories<EFTestScope>();
            return base.ConfigureApplication(application, name);
        }
    }
}
