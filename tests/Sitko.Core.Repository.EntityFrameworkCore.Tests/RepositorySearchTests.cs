using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sitko.Core.Repository.EntityFrameworkCore.Tests.Data;
using Sitko.Core.Repository.Search;
using Sitko.Core.Repository.Tests.Data;
using Sitko.Core.Search;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.Repository.EntityFrameworkCore.Tests
{
    public class RepositorySearchTests(ITestOutputHelper testOutputHelper) : BaseTest<RepositorySearchTestScope>(testOutputHelper)
    {
        [Fact]
        public async Task SearchAsyncPreservesSearcherOrderWhenRepositoryLoadsInInsertionOrder()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var scope = await GetScopeAsync();
            var repository = scope.GetService<IRepository<TestModel, Guid>>();
            var searcher = scope.GetService<RecordingSearcher>();
            var a = Guid.Parse("00000000-0000-0000-0000-000000000201");
            var b = Guid.Parse("00000000-0000-0000-0000-000000000202");
            Assert.True((await repository.AddAsync(new TestModel { Id = a, FooId = 7 }, cancellationToken)).IsSuccess);
            Assert.True((await repository.AddAsync(new TestModel { Id = b, FooId = 7 }, cancellationToken)).IsSuccess);
            Guid[] repositoryOrder = [a, b];
            Assert.Equal(repositoryOrder,
                (await repository.GetByIdsAsync([b, a], cancellationToken)).Select(static model => model.Id));

            searcher.SearchResults = [b, a];
            var results = await scope.GetService<ISearchProvider<TestModel, Guid, RepositoryTestSearchModel>>()
                .SearchAsync("known term", cancellationToken: cancellationToken);

            Guid[] searchOrder = [b, a];
            Assert.Equal(searchOrder, results.Select(static result => result.Entity.Id));
        }
    }

    public class RepositorySearchTestScope : BaseEFTestScope
    {
        protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name)
        {
            _ = hostBuilder.Configuration.AddEnvironmentVariables();
            _ = hostBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tests:UsePostgres"] = "false"
            });
            AddDbContext<TestDbContext>(hostBuilder, name, ConfigureInMemoryDatabaseModule,
                ConfigurePostgresDatabaseModule, InitDbContextAsync);
            _ = hostBuilder.AddEFRepositories<TestModel>(static options =>
                options.AddRepository<TestRepository>());
            _ = hostBuilder.Services.AddSingleton<RecordingSearcher>();
            _ = hostBuilder.Services.AddSingleton<ISearcher<RepositoryTestSearchModel>>(static provider =>
                provider.GetRequiredService<RecordingSearcher>());
            _ = hostBuilder.Services.AddScoped<RepositoryTestSearchProvider>();
            _ = hostBuilder.Services.AddScoped<ISearchProvider<TestModel, Guid, RepositoryTestSearchModel>>(static provider =>
                provider.GetRequiredService<RepositoryTestSearchProvider>());
            return hostBuilder;
        }
    }

    public class RepositoryTestSearchModel : BaseSearchModel;

    public class RepositoryTestSearchProvider(
        ILogger<BaseRepositorySearchProvider<TestModel, Guid, RepositoryTestSearchModel>> logger,
        IRepository<TestModel, Guid> repository,
        ISearcher<RepositoryTestSearchModel> searcher)
        : BaseRepositorySearchProvider<TestModel, Guid, RepositoryTestSearchModel>(logger, repository, searcher)
    {
        protected override Guid ParseId(string id) => Guid.Parse(id);

        protected override Task<RepositoryTestSearchModel[]> GetSearchModelsAsync(TestModel[] entities,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    public class RecordingSearcher : ISearcher<RepositoryTestSearchModel>
    {
        public Guid[] SearchResults { get; set; } = [];

        public Task<bool> AddOrUpdateAsync(string indexName, IEnumerable<RepositoryTestSearchModel> searchModels,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<SearcherEntity<RepositoryTestSearchModel>[]> SearchAsync(string indexName, string term,
            SearchOptions? searchOptions, CancellationToken cancellationToken = default) =>
            Task.FromResult(SearchResults.Select(static id => new SearcherEntity<RepositoryTestSearchModel>(
                new RepositoryTestSearchModel { Id = id.ToString() },
                new Dictionary<string, IReadOnlyCollection<string>>())).ToArray());

        public Task<bool> DeleteAsync(string indexName, IEnumerable<RepositoryTestSearchModel> searchModels,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> DeleteAsync(string indexName, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<long> CountAsync(string indexName, string term, SearchOptions? searchOptions,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<SearcherEntity<RepositoryTestSearchModel>[]> GetSimilarAsync(string indexName, string id,
            SearchOptions? searchOptions, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task InitAsync(string indexName, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
