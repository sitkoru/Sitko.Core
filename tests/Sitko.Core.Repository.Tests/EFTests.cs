using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Db.Postgres;
using Sitko.Core.Repository.EntityFrameworkCore;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

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

            var repository = scope.Get<IRepository<TestModel, Guid>>();

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
            var validator = scope.GetAll<IValidator>();
            Assert.NotEmpty(validator);

            var typedValidator = scope.GetAll<IValidator<TestModel>>();
            Assert.NotEmpty(typedValidator);

            var repository = scope.Get<IRepository<TestModel, Guid>>();

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

            var repository = scope.Get<IRepository<TestModel, Guid>>();

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

            var repository = scope.Get<IRepository<TestModel, Guid>>();

            Assert.NotNull(repository);

            var items = await repository.GetAllAsync(q => q.WhereByString(json));

            Assert.NotEqual(0, items.itemsCount);
            Assert.Single(items.items);
        }

        [Fact]
        public async Task IncludeSingle()
        {
            var scope = await GetScopeAsync();

            var repository = scope.Get<IRepository<BarModel, Guid>>();

            Assert.NotNull(repository);

            var item = await repository.GetAsync(query => query.Include(e => e.Test));
            Assert.NotNull(item);
            Assert.NotNull(item!.Test);
            Assert.Equal(1, item.Test.FooId);
        }

        [Fact]
        public async Task IncludeCollection()
        {
            var scope = await GetScopeAsync();

            var repository = scope.Get<IRepository<TestModel, Guid>>();

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

            var repository = scope.Get<IRepository<TestModel, Guid>>();
            Assert.NotNull(repository);

            var item = await repository.GetAsync(query => query
                .Include(e => e.Bars)
                .ThenInclude(e => e.Foos).ThenInclude(f => f.Bar).ThenInclude(b => b.Test));
            Assert.NotNull(item);
            Assert.NotNull(item!.Bars);
            Assert.NotEmpty(item.Bars);
            Assert.Single(item.Bars);
            var bar = item.Bars.First();
            Assert.Equal(item.Id, bar.TestId);
            Assert.NotEmpty(bar.Foos);
            Assert.Single(bar.Foos);
            var foo = bar.Foos.First();
            Assert.NotNull(foo.Bar);
            Assert.NotNull(foo.Bar.Test);
        }
        
        [Fact]
        public async Task ThreadSafe()
        {
            var scope = await GetScopeAsync();

            var threadSafeRepository = scope.Get<IRepository<TestModel, Guid>>();

            var tasks = new List<Task> {threadSafeRepository.GetAllAsync(), threadSafeRepository.GetAllAsync()};

            await Task.WhenAll(tasks);

            foreach (var task in tasks)
            {
                Assert.True(task.IsCompletedSuccessfully);
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
                new() {Id = Guid.NewGuid(), FooId = 1},
                new() {Id = Guid.NewGuid(), FooId = 2},
                new() {Id = Guid.NewGuid(), FooId = 3},
                new() {Id = Guid.NewGuid(), FooId = 4},
                new() {Id = Guid.NewGuid(), FooId = 5},
                new() {Id = Guid.NewGuid(), FooId = 5}
            };
            modelBuilder.Entity<TestModel>().HasData(testModels);
            var barModel = new BarModel {Id = Guid.NewGuid(), TestId = testModels.First().Id};
            modelBuilder.Entity<BarModel>().HasData(barModel);
            modelBuilder.Entity<FooModel>().HasData(new FooModel {Id = Guid.NewGuid(), BarId = barModel.Id});
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
        public TestModelValidator()
        {
            RuleFor(m => m.Status).NotEqual(TestStatus.Error).WithMessage("Status can't be error");
        }
    }

    public enum TestStatus
    {
        Enabled, Disabled, Error
    }

    public class BarModel : Entity<Guid>
    {
        public Guid TestId { get; set; }
        [ForeignKey(nameof(TestId))] public TestModel Test { get; set; } = null!;

        [InverseProperty(nameof(FooModel.Bar))]
        public List<FooModel> Foos { get; set; } = new();
    }

    public class FooModel : Entity<Guid>
    {
        public Guid BarId { get; set; }
        [ForeignKey(nameof(BarId))] public BarModel Bar { get; set; } = null!;
    }

    public class TestRepository : EFRepository<TestModel, Guid, TestDbContext>
    {
        public TestRepository(EFRepositoryContext<TestModel, Guid, TestDbContext> repositoryContext) : base(repositoryContext)
        {
        }
    }

    public class BarRepository : EFRepository<BarModel, Guid, TestDbContext>
    {
        public BarRepository(EFRepositoryContext<BarModel, Guid, TestDbContext> repositoryContext) : base(repositoryContext)
        {
        }
    }

    public class FooRepository : EFRepository<FooModel, Guid, TestDbContext>
    {
        public FooRepository(EFRepositoryContext<FooModel, Guid, TestDbContext> repositoryContext) : base(repositoryContext)
        {
        }
    }

    public abstract class BaseEFTestScope : DbBaseTestScope<BaseEFTestScope, TestDbContext>
    {
    }

    public class EFTestScope : BaseEFTestScope
    {
        protected override TestApplication ConfigureApplication(TestApplication application, string name)
        {
            return base.ConfigureApplication(application, name)
                .AddModule<TestApplication, EFRepositoriesModule<EFTestScope>, EfRepositoriesModuleOptions>();
        }
    }
}
