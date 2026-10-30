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
    public class ReindexTests(ITestOutputHelper testOutputHelper) : BaseTest<ReindexTestScope>(testOutputHelper)
    {
        [Fact]
        public async Task ReindexAsyncIndexesEachEntityExactlyOnceAcrossPages()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var scope = await GetScopeAsync();
            var repository = scope.GetService<IRepository<TestModel, Guid>>();
            var searcher = scope.GetService<ReindexRecordingSearcher>();
            var ids = new[]
            {
                Guid.Parse("00000000-0000-0000-0000-000000000101"),
                Guid.Parse("00000000-0000-0000-0000-000000000102"),
                Guid.Parse("00000000-0000-0000-0000-000000000103"),
                Guid.Parse("00000000-0000-0000-0000-000000000104"),
                Guid.Parse("00000000-0000-0000-0000-000000000105")
            };
            foreach (var id in ids)
            {
                Assert.True((await repository.AddAsync(new TestModel { Id = id, FooId = 7 }, cancellationToken)).IsSuccess);
            }

            await scope.GetService<IRepositorySearchProvider<TestModel>>().ReindexAsync(2, cancellationToken);

            Assert.Equal(5, searcher.IndexedIds.Count);
            Assert.Equal(ids, searcher.IndexedIds.Order());
        }
    }

    public class ReindexTestScope : BaseEFTestScope
    {
        protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name)
        {
            _ = base.ConfigureApplication(hostBuilder, name).AddEFRepositories<TestModel>(static options =>
                options.AddRepository<TestRepository>());
            _ = hostBuilder.Services.AddSingleton<ReindexRecordingSearcher>();
            _ = hostBuilder.Services.AddSingleton<ISearcher<ReindexTestSearchModel>>(static provider =>
                provider.GetRequiredService<ReindexRecordingSearcher>());
            _ = hostBuilder.Services.AddScoped<IRepositorySearchProvider<TestModel>, ReindexTestSearchProvider>();
            return hostBuilder;
        }
    }

    public class ReindexTestSearchModel : BaseSearchModel;

    public class ReindexTestSearchProvider(
        ILogger<BaseRepositorySearchProvider<TestModel, Guid, ReindexTestSearchModel>> logger,
        IRepository<TestModel, Guid> repository,
        ISearcher<ReindexTestSearchModel> searcher)
        : BaseRepositorySearchProvider<TestModel, Guid, ReindexTestSearchModel>(logger, repository, searcher)
    {
        protected override Guid ParseId(string id) => Guid.Parse(id);

        protected override Task<ReindexTestSearchModel[]> GetSearchModelsAsync(TestModel[] entities,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(entities.Select(static entity => new ReindexTestSearchModel
            {
                Id = entity.Id.ToString(),
                Title = "known term",
                Content = "known term",
                Url = "/known",
                Date = DateTimeOffset.UnixEpoch,
                Tags = []
            }).ToArray());
    }

    public class ReindexRecordingSearcher : ISearcher<ReindexTestSearchModel>
    {
        public List<Guid> IndexedIds { get; } = [];

        public Task<bool> AddOrUpdateAsync(string indexName, IEnumerable<ReindexTestSearchModel> searchModels,
            CancellationToken cancellationToken = default)
        {
            IndexedIds.AddRange(searchModels.Select(static model => Guid.Parse(model.Id)));
            return Task.FromResult(true);
        }

        public Task<SearcherEntity<ReindexTestSearchModel>[]> SearchAsync(string indexName, string term,
            SearchOptions? searchOptions, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> DeleteAsync(string indexName, IEnumerable<ReindexTestSearchModel> searchModels,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> DeleteAsync(string indexName, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<long> CountAsync(string indexName, string term, SearchOptions? searchOptions,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<SearcherEntity<ReindexTestSearchModel>[]> GetSimilarAsync(string indexName, string id,
            SearchOptions? searchOptions, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task InitAsync(string indexName, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
