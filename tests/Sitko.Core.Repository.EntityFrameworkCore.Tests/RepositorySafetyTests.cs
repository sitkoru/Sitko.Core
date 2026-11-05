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
        public async Task RejectedUpdateDoesNotPersistWithNextSuccessfulUpdate()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var scope = await GetScopeAsync();
            var repository = scope.GetService<IRepository<TestModel, Guid>>();
            var rejected = await repository.GetAsync(static query => query.Where(static model => model.FooId == 1),
                cancellationToken);
            var accepted = await repository.GetAsync(static query => query.Where(static model => model.FooId == 2),
                cancellationToken);
            var pending = await repository.GetAsync(static query => query.Where(static model => model.FooId == 3),
                cancellationToken);
            Assert.NotNull(pending);
            Assert.NotNull(rejected);
            Assert.NotNull(accepted);
            Assert.Equal(TestStatus.Enabled, rejected.Status);

            pending.Status = TestStatus.Disabled;
            rejected.Status = TestStatus.Error;
            var rejectedResult = await repository.UpdateAsync(rejected, cancellationToken);
            Assert.False(rejectedResult.IsSuccess);
            var error = Assert.Single(rejectedResult.Errors);
            Assert.Equal(nameof(TestModel.Status), error.PropertyName);
            Assert.Equal("Status can't be error", error.ErrorMessage);

            accepted.Status = TestStatus.Disabled;
            var acceptedResult = await repository.UpdateAsync(accepted, cancellationToken);
            Assert.True(acceptedResult.IsSuccess);

            using var verificationScope = scope.CreateScope();
            var verificationRepository = verificationScope.ServiceProvider.GetRequiredService<IRepository<TestModel, Guid>>();
            var savedAccepted = await verificationRepository.GetByIdAsync(accepted.Id, cancellationToken);
            Assert.NotNull(savedAccepted);
            Assert.Equal(TestStatus.Disabled, savedAccepted.Status);
            var savedPending = await verificationRepository.GetByIdAsync(pending.Id, cancellationToken);
            Assert.NotNull(savedPending);
            Assert.Equal(TestStatus.Disabled, savedPending.Status);
            var savedRejected = await verificationRepository.GetByIdAsync(rejected.Id, cancellationToken);
            Assert.NotNull(savedRejected);
            Assert.Equal(TestStatus.Enabled, savedRejected.Status);

            rejected.Status = TestStatus.Disabled;
            var retriedResult = await repository.UpdateAsync(rejected, cancellationToken);
            Assert.True(retriedResult.IsSuccess);
            using var retryVerificationScope = scope.CreateScope();
            var retryVerificationRepository = retryVerificationScope.ServiceProvider
                .GetRequiredService<IRepository<TestModel, Guid>>();
            var savedRetried = await retryVerificationRepository.GetByIdAsync(rejected.Id, cancellationToken);
            Assert.NotNull(savedRetried);
            Assert.Equal(TestStatus.Disabled, savedRetried.Status);
        }

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
