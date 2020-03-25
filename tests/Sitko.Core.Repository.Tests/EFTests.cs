using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.UserSecrets;
using Sitko.Core.Repository.EntityFrameworkCore;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

[assembly: UserSecretsId("test")]

namespace Sitko.Core.Repository.Tests
{
    public class EFTests : BaseTest<EFTestScope>
    {
        public EFTests([NotNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task JsonConditions()
        {
            var json = "[{\"conditions\":[{\"property\":\"fooId\",\"operator\":1,\"value\":1}]}]";

            var scope = GetScope();

            var repository = scope.Get<IRepository<TestModel, Guid>>();

            Assert.NotNull(repository);

            var items = await repository.GetAllAsync(q => q.WhereByString(json));

            Assert.NotEqual(0, items.itemsCount);
            Assert.Single(items.items);
        }

        [Fact]
        public async Task IncludeSingle()
        {
            var scope = GetScope();

            var repository = scope.Get<IRepository<BarModel, Guid>>();

            Assert.NotNull(repository);

            var item = await repository.GetAsync(query => query.Include(e => e.Test));
            Assert.NotNull(item);
            Assert.NotNull(item.Test);
            Assert.Equal(1, item.Test.FooId);
        }

        [Fact]
        public async Task IncludeCollection()
        {
            var scope = GetScope();

            var repository = scope.Get<IRepository<TestModel, Guid>>();

            Assert.NotNull(repository);

            var item = await repository.GetAsync(query => query.Include(e => e.Bars));
            Assert.NotNull(item);
            Assert.NotNull(item.Bars);
            Assert.NotEmpty(item.Bars);
            Assert.Single(item.Bars);
            Assert.Equal(item.Id, item.Bars.First().TestId);
        }

        [Fact]
        public async Task ThenInclude()
        {
            var scope = GetScope();

            var repository = scope.Get<IRepository<TestModel, Guid>>();
            Assert.NotNull(repository);

            var item = await repository.GetAsync(query => query
                .Include(e => e.Bars)
                .ThenInclude(e => e.Foos).ThenInclude(f => f.Bar).ThenInclude(b => b.Test));
            Assert.NotNull(item);
            Assert.NotNull(item.Bars);
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
    }

    public class TestDbContext : DbContext
    {
        public DbSet<TestModel> TestModels { get; set; }

        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            var testModels = new List<TestModel>
            {
                new TestModel {Id = Guid.NewGuid(), FooId = 1},
                new TestModel {Id = Guid.NewGuid(), FooId = 2},
                new TestModel {Id = Guid.NewGuid(), FooId = 3},
                new TestModel {Id = Guid.NewGuid(), FooId = 4},
                new TestModel {Id = Guid.NewGuid(), FooId = 5},
                new TestModel {Id = Guid.NewGuid(), FooId = 5}
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

        [InverseProperty(nameof(BarModel.Test))]
        public List<BarModel> Bars { get; set; }
    }

    public class BarModel : Entity<Guid>
    {
        public Guid TestId { get; set; }
        [ForeignKey(nameof(TestId))] public TestModel Test { get; set; }

        [InverseProperty(nameof(FooModel.Bar))]
        public List<FooModel> Foos { get; set; }
    }

    public class FooModel : Entity<Guid>
    {
        public Guid BarId { get; set; }
        [ForeignKey(nameof(BarId))] public BarModel Bar { get; set; }
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

    public class EFTestScope : DbBaseTestScope<EFTestScope, TestDbContext>
    {
        protected override TestApplication ConfigureApplication(TestApplication application, string name)
        {
            return base.ConfigureApplication(application, name).AddModule<EFRepositoriesModule<EFTestScope>>();
        }
    }
}
