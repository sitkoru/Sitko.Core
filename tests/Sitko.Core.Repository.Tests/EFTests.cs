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
using Sitko.Core.Db.Postgres;
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
            await repository.RefreshAsync(item);
            Assert.Equal(TestStatus.Disabled, item.Status);
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
        public async Task IncludeWithPagination()
        {
            var scope = await GetScopeAsync();

            var repository = scope.GetService<IRepository<TestModel, Guid>>();

            Assert.NotNull(repository);

            var item = await repository.GetAllAsync(query => query.Include(e => e.Bars).Take(1));
            Assert.Single(item.items);
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
                bar = await repository2.GetAsync(q => q.Where(b => b.Id == originalBar!.Id));
                Assert.NotNull(bar);
            }

            Assert.Null(bar!.Baz);
            bar.Baz = "123";

            using (var scope2 = scope.CreateScope())
            {
                var repository2 = scope2.ServiceProvider.GetRequiredService<BarRepository>();
                var updateResult = await repository2.UpdateAsync(bar, originalBar);
                Assert.True(updateResult.IsSuccess);
                Assert.Single(updateResult.Changes);
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
                var updateResult = await repository2.DeleteAsync(originalBar!);
                Assert.True(updateResult);
            }

            using (var finalScope = scope.CreateScope())
            {
                var repository = finalScope.ServiceProvider.GetRequiredService<BarRepository>();
                var updatedBar =
                    await repository.GetAsync(q => q.Where(b => b.Id == originalBar!.Id));
                Assert.Null(updatedBar);
            }
        }

        [Fact]
        public async Task SkipNavigations()
        {
            var scope = await GetScopeAsync();
            BarModel? originalBar;
            using (var scope1 = scope.CreateScope())
            {
                var dbContext = scope1.ServiceProvider.GetRequiredService<TestDbContext>();
                originalBar = await dbContext.Set<BarModel>().Where(b => b.TestId != null).Include(b => b.Foos)
                    .Include(b => b.BazModels)
                    .FirstOrDefaultAsync();
                Assert.NotNull(originalBar);
            }

            Assert.Single(originalBar!.Foos);
            var foo = originalBar.Foos.First();
            Assert.Empty(foo.BazModels);

            using (var scope2 = scope.CreateScope())
            {
                var dbContext = scope2.ServiceProvider.GetRequiredService<TestDbContext>();
                var bazModels = await dbContext.Set<BazModel>().Where(b => b.Foos.Contains(foo)).ToListAsync();
                Assert.NotEmpty(bazModels);
                Assert.Empty(foo.BazModels);
            }
        }

        [Fact]
        public async Task DisconnectedOneToOne()
        {
            var scope = await GetScopeAsync();
            BarModel? originalBar;
            using (var scope1 = scope.CreateScope())
            {
                var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
                originalBar = await repository1.GetAsync(q => q.Where(b => b.TestId == null));
                Assert.NotNull(originalBar);
            }

            BarModel? bar;
            using (var scope1 = scope.CreateScope())
            {
                var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
                bar = await repository1.GetAsync(q => q.Where(b => b.Id == originalBar!.Id));
                Assert.NotNull(bar);
            }

            Assert.Null(bar!.Test);
            Assert.Equal(default, bar.TestId);

            AddOrUpdateOperationResult<TestModel, Guid> newTestResult;
            using (var scope2 = scope.CreateScope())
            {
                var repository2 = scope2.ServiceProvider.GetRequiredService<IRepository<TestModel, Guid>>();
                newTestResult = await repository2.AddAsync(await repository2.NewAsync());
            }

            Assert.True(newTestResult.IsSuccess);

            bar.Test = newTestResult.Entity;

            using (var scope3 = scope.CreateScope())
            {
                var repository3 = scope3.ServiceProvider.GetRequiredService<BarRepository>();
                var updateResult =
                    await repository3.UpdateAsync(bar, originalBar);
                Assert.True(updateResult.IsSuccess);
                Assert.Equal(2, updateResult.Changes.Length);
            }

            using (var finalScope = scope.CreateScope())
            {
                var repository = finalScope.ServiceProvider.GetRequiredService<BarRepository>();
                var updatedBar =
                    await repository.GetAsync(q => q.Where(b => b.Id == bar.Id));
                Assert.Equal(newTestResult.Entity.Id, updatedBar!.TestId);
            }
        }

        [Fact]
        public async Task DisconnectedOneToOneViaProperty()
        {
            var scope = await GetScopeAsync();
            BarModel? originalBar;
            using (var scope1 = scope.CreateScope())
            {
                var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
                originalBar = await repository1.GetAsync(q => q.Where(b => b.TestId == null));
                Assert.NotNull(originalBar);
            }

            BarModel? bar;
            using (var scope1 = scope.CreateScope())
            {
                var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
                bar = await repository1.GetAsync(q => q.Where(b => b.Id == originalBar!.Id));
                Assert.NotNull(bar);
            }

            Assert.Null(bar!.Test);
            Assert.Equal(default, bar.TestId);

            AddOrUpdateOperationResult<TestModel, Guid> newTestResult;
            using (var scope2 = scope.CreateScope())
            {
                var repository2 = scope2.ServiceProvider.GetRequiredService<IRepository<TestModel, Guid>>();
                newTestResult = await repository2.AddAsync(await repository2.NewAsync());
            }

            Assert.True(newTestResult.IsSuccess);

            bar.TestId = newTestResult.Entity.Id;


            using (var scope3 = scope.CreateScope())
            {
                var repository3 = scope3.ServiceProvider.GetRequiredService<BarRepository>();
                var updateResult =
                    await repository3.UpdateAsync(bar, originalBar);
                Assert.True(updateResult.IsSuccess);
                Assert.Equal(2, updateResult.Changes.Length);
            }

            using (var finalScope = scope.CreateScope())
            {
                var repository = finalScope.ServiceProvider.GetRequiredService<BarRepository>();
                var updatedBar =
                    await repository.GetAsync(q => q.Where(b => b.Id == bar.Id));
                Assert.Equal(newTestResult.Entity.Id, updatedBar!.TestId);
            }
        }

        [Fact]
        public async Task DisconnectedOneToOneAdd()
        {
            var scope = await GetScopeAsync();
            var originalBar = new BarModel { Id = Guid.NewGuid() };
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
                var updateResult = await repository3.AddAsync(originalBar);
                Assert.True(updateResult.IsSuccess);
                Assert.Empty(updateResult.Changes);
            }

            using (var finalScope = scope.CreateScope())
            {
                var repository = finalScope.ServiceProvider.GetRequiredService<BarRepository>();
                var updatedBar =
                    await repository.GetAsync(q => q.Where(b => b.Id == originalBar.Id));
                Assert.NotNull(updatedBar);
                Assert.Equal(newTestResult.Entity.Id, updatedBar!.TestId);
            }
        }

        [Fact]
        public async Task DisconnectedOneToOneUpdateEntity()
        {
            var scope = await GetScopeAsync();
            BarModel? originalBar;
            using (var scope1 = scope.CreateScope())
            {
                var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
                originalBar = await repository1.GetAsync(q => q.Where(b => b.TestId != null).Include(b => b.Test));
                Assert.NotNull(originalBar);
            }

            BarModel? bar;
            using (var scope1 = scope.CreateScope())
            {
                var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
                bar = await repository1.GetAsync(q => q.Where(b => b.Id == originalBar!.Id).Include(b => b.Test));
                Assert.NotNull(bar);
            }


            Assert.NotNull(bar!.Test);
            bar.Test!.FooId = 10;

            using (var scope3 = scope.CreateScope())
            {
                var repository3 = scope3.ServiceProvider.GetRequiredService<BarRepository>();
                var updateResult = await repository3.UpdateAsync(bar, originalBar);
                Assert.True(updateResult.IsSuccess);
                Assert.Single(updateResult.Changes);
            }

            using (var finalScope = scope.CreateScope())
            {
                var repository = finalScope.ServiceProvider.GetRequiredService<BarRepository>();
                var updatedBar =
                    await repository.GetAsync(q => q.Where(b => b.Id == bar.Id).Include(b => b.Test));
                Assert.Equal(10, updatedBar!.Test!.FooId);
            }
        }


        [Fact]
        public async Task DisconnectedOneToOneDeleteEntity()
        {
            var scope = await GetScopeAsync();
            BarModel? originalBar;
            using (var scope1 = scope.CreateScope())
            {
                var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
                originalBar = await repository1.GetAsync(q => q.Where(b => b.TestId != null).Include(b => b.Test));
                Assert.NotNull(originalBar);
            }

            BarModel? bar;
            using (var scope1 = scope.CreateScope())
            {
                var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
                bar = await repository1.GetAsync(q => q.Where(b => b.Id == originalBar!.Id).Include(b => b.Test));
                Assert.NotNull(bar);
            }

            Assert.NotNull(bar!.Test);
            bar.Test = null;

            using (var scope3 = scope.CreateScope())
            {
                var repository3 = scope3.ServiceProvider.GetRequiredService<BarRepository>();
                var updateResult =
                    await repository3.UpdateAsync(bar, originalBar);
                Assert.True(updateResult.IsSuccess);
                Assert.Equal(2, updateResult.Changes.Length);
            }

            using (var finalScope = scope.CreateScope())
            {
                var repository = finalScope.ServiceProvider.GetRequiredService<BarRepository>();
                var updatedBar =
                    await repository.GetAsync(q => q.Where(b => b.Id == bar.Id).Include(b => b.Foos));
                Assert.Null(updatedBar!.Test);
                Assert.Null(updatedBar.TestId);
            }
        }

        [Fact]
        public async Task DisconnectedOneToMany()
        {
            var scope = await GetScopeAsync();
            BarModel? originalBar;
            using (var scope1 = scope.CreateScope())
            {
                var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
                originalBar = await repository1.GetAsync(q => q.Where(b => !b.Foos.Any()));
                Assert.NotNull(originalBar);
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
                var updateResult = await repository3.UpdateAsync(originalBar);
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
            using (var scope1 = scope.CreateScope())
            {
                var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
                originalBar = await repository1.GetAsync(q => q.Where(b => !b.Foos.Any()));
                Assert.NotNull(originalBar);
            }

            Assert.Empty(originalBar!.Foos);

            var foo = new FooModel();

            originalBar.Foos.Add(foo);

            using (var scope3 = scope.CreateScope())
            {
                var repository3 = scope3.ServiceProvider.GetRequiredService<BarRepository>();
                var updateResult = await repository3.UpdateAsync(originalBar);
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
            using (var scope1 = scope.CreateScope())
            {
                var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
                originalBar = await repository1.GetAsync(q => q.Where(b => b.Foos.Any()).Include(b => b.Foos));
                Assert.NotNull(originalBar);
            }

            Assert.NotEmpty(originalBar!.Foos);
            var foo = originalBar.Foos.OrderBy(_ => Guid.NewGuid()).First(); // change random
            var newText = Guid.NewGuid().ToString();
            foo.FooText = newText;

            using (var scope3 = scope.CreateScope())
            {
                var repository3 = scope3.ServiceProvider.GetRequiredService<BarRepository>();
                var updateResult = await repository3.UpdateAsync(originalBar);
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
            using (var scope1 = scope.CreateScope())
            {
                var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
                originalBar = await repository1.GetAsync(q => q.Where(b => b.Foos.Any()));
                Assert.NotNull(originalBar);
            }

            BarModel? bar;
            using (var scope1 = scope.CreateScope())
            {
                var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
                bar = await repository1.GetAsync(q => q.Where(b => b.Id==originalBar!.Id));
                Assert.NotNull(bar);
            }

            Assert.NotEmpty(bar!.Foos);
            var count = bar.Foos.Count;
            var foo1 = new FooModel();
            var foo2 = new FooModel();
            bar.Foos.Remove(bar.Foos.OrderBy(_ => Guid.NewGuid()).First());
            bar.Foos.Add(foo1);
            bar.Foos.Add(foo2);

            using (var scope3 = scope.CreateScope())
            {
                var repository3 = scope3.ServiceProvider.GetRequiredService<BarRepository>();
                var updateResult = await repository3.UpdateAsync(bar, originalBar);
                Assert.True(updateResult.IsSuccess);
                Assert.Single(updateResult.Changes);
            }

            using (var finalScope = scope.CreateScope())
            {
                var repository = finalScope.ServiceProvider.GetRequiredService<BarRepository>();
                var updatedBar =
                    await repository.GetAsync(q => q.Where(b => b.Id == bar.Id).Include(b => b.Foos));
                Assert.NotEmpty(updatedBar!.Foos);
                Assert.Equal(count + 1, updatedBar.Foos.Count);
            }
        }

        [Fact]
        public async Task DisconnectedOneToManyDeleteEntity()
        {
            var scope = await GetScopeAsync();
            BarModel? originalBar;
            using (var scope1 = scope.CreateScope())
            {
                var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
                originalBar = await repository1.GetAsync(q => q.Where(b => b.Foos.Any()).Include(b => b.Foos));
                Assert.NotNull(originalBar);
            }

            BarModel? bar;
            using (var scope1 = scope.CreateScope())
            {
                var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
                bar = await repository1.GetAsync(q => q.Where(b => b.Id == originalBar!.Id).Include(b => b.Foos));
                Assert.NotNull(bar);
            }


            Assert.NotNull(bar);
            Assert.NotEmpty(bar!.Foos);
            var count = bar.Foos.Count;
            Assert.Equal(3, count);

            bar.Foos.Remove(bar.Foos.OrderBy(_ => Guid.NewGuid()).First()); // delete random
            using (var scope3 = scope.CreateScope())
            {
                var repository3 = scope3.ServiceProvider.GetRequiredService<BarRepository>();
                var updateResult =
                    await repository3.UpdateAsync(bar, originalBar);
                Assert.True(updateResult.IsSuccess);
                Assert.Single(updateResult.Changes);
            }

            using (var finalScope = scope.CreateScope())
            {
                var repository = finalScope.ServiceProvider.GetRequiredService<BarRepository>();
                var updatedBar =
                    await repository.GetAsync(q => q.Where(b => b.Id == bar.Id).Include(b => b.Foos));
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
            modelBuilder.RegisterJsonEnumerableConversion<BarModel, BaseJsonModel, List<BaseJsonModel>>(model =>
                model.JsonModels, "JsonModels");
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

        public List<BazModel> BazModels { get; set; } = new();

        public List<BaseJsonModel> JsonModels { get; set; } = new();
        public string? Baz { get; set; }
    }

    public class BazModel : Entity<Guid>
    {
        public string Baz { get; set; } = "";
        public List<FooModel> Foos { get; set; } = new();
        public List<BarModel> Bars { get; set; } = new();
    }

    public abstract record BaseJsonModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
    }

    public record JsonModelFoo : BaseJsonModel
    {
        public string Foo { get; set; } = "";
    }

    public record JsonModelBar : BaseJsonModel
    {
        public string Bar { get; set; } = "";
    }

    public class FooModel : Entity<Guid>
    {
        public string? FooText { get; set; }
        public Guid? BarId { get; set; }
        [ForeignKey(nameof(BarId))] public BarModel? Bar { get; set; } = null!;

        public List<BazModel> BazModels { get; set; } = new();
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

        protected override IQueryable<BarModel> AddIncludes(IQueryable<BarModel> query) =>
            base.AddIncludes(query).Include(b => b.BazModels).Include(b => b.Foos).ThenInclude(f => f.BazModels);
    }

    public class FooRepository : EFRepository<FooModel, Guid, TestDbContext>
    {
        public FooRepository(EFRepositoryContext<FooModel, Guid, TestDbContext> repositoryContext) : base(
            repositoryContext)
        {
        }

        protected override IQueryable<FooModel> AddIncludes(IQueryable<FooModel> query) =>
            base.AddIncludes(query).Include(f => f.BazModels);
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

        protected override async Task InitDbContextAsync(TestDbContext dbContext)
        {
            await base.InitDbContextAsync(dbContext);
            var testModels = new List<TestModel>
            {
                new() { Id = Guid.NewGuid(), FooId = 1 },
                new() { Id = Guid.NewGuid(), FooId = 2 },
                new() { Id = Guid.NewGuid(), FooId = 3 },
                new() { Id = Guid.NewGuid(), FooId = 4 },
                new() { Id = Guid.NewGuid(), FooId = 5 },
                new() { Id = Guid.NewGuid(), FooId = 5 }
            };
            await dbContext.AddRangeAsync(testModels);

            var barModels = new List<BarModel>()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    TestId = testModels.First().Id,
                    JsonModels = new List<BaseJsonModel> { new JsonModelBar(), new JsonModelFoo() }
                },
                new() { Id = Guid.NewGuid() },
                new() { Id = Guid.NewGuid() }
            };
            await dbContext.AddRangeAsync(barModels);

            var fooModels = new[]
            {
                new FooModel { Id = Guid.NewGuid(), BarId = barModels[0].Id, FooText = "123" },
                new FooModel { Id = Guid.NewGuid(), BarId = barModels[1].Id, FooText = "456" },
                new FooModel { Id = Guid.NewGuid(), BarId = barModels[1].Id, FooText = "789" },
                new FooModel { Id = Guid.NewGuid(), BarId = barModels[1].Id, FooText = "012" }
            };
            var bazModels = new List<BazModel>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Baz = "1",
                    Bars = barModels.Take(2).ToList(),
                    Foos = fooModels.Take(2).ToList()
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Baz = "2",
                    Bars = barModels.Take(5).ToList(),
                    Foos = fooModels.Take(2).ToList()
                },
                new() { Id = Guid.NewGuid(), Baz = "3", Foos = fooModels.Take(2).ToList() },
                new() { Id = Guid.NewGuid(), Baz = "4" },
                new() { Id = Guid.NewGuid(), Baz = "5" },
                new() { Id = Guid.NewGuid(), Baz = "6" }
            };
            await dbContext.Set<BazModel>().AddRangeAsync(bazModels);
            await dbContext.Set<FooModel>().AddRangeAsync(fooModels);
            await dbContext.SaveChangesAsync();
        }
    }
}
