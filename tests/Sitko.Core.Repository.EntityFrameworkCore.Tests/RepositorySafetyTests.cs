using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.Repository.EntityFrameworkCore.Tests.Data;
using Sitko.Core.Repository.Tests.Data;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.Repository.EntityFrameworkCore.Tests
{
    public class RepositorySafetyTests(ITestOutputHelper testOutputHelper) : BaseTest<EFTestScope>(testOutputHelper)
    {
        [Fact]
        public async Task RollbackTransactionDoesNotPersistAddedEntity()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var scope = await GetScopeAsync();
            Assert.True(scope.Config.UsePostgres, "Transaction rollback requires PostgreSQL.");
            var repository = scope.GetService<IRepository<TestModel, Guid>>();
            var id = Guid.Parse("cc6dc846-98e9-47b5-a6dc-3b9e17235891");
            Assert.Null(await repository.GetByIdAsync(id, cancellationToken));

            Assert.True(await repository.BeginTransactionAsync(cancellationToken));
            var result = await repository.AddAsync(new TestModel { Id = id, FooId = 42 }, cancellationToken);
            Assert.True(result.IsSuccess);
            Assert.NotNull(await repository.GetByIdAsync(id, cancellationToken));
            Assert.True(await repository.RollbackTransactionAsync(cancellationToken));

            using var verificationScope = scope.CreateScope();
            var verificationRepository = verificationScope.ServiceProvider.GetRequiredService<IRepository<TestModel, Guid>>();
            Assert.Null(await verificationRepository.GetByIdAsync(id, cancellationToken));
        }
    }
}
