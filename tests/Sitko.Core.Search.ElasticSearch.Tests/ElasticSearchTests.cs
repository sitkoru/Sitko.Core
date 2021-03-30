using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sitko.Core.App;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Search.ElasticSearch.Tests
{
    public class ElasticSearchTests : BaseTest<ElasticSearchTestScope>
    {
        public ElasticSearchTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task Search()
        {
            var scope = await GetScopeAsync();
            var provider = scope.Get<TestModelProvider>();
            var searchProvider = scope.Get<ISearchProvider<TestModel, Guid>>();
            await searchProvider.DeleteIndexAsync();
            await searchProvider.InitAsync();

            var fooModel = new TestModel
            {
                Id = Guid.NewGuid(),
                Title = "MMI",
                Description =
                    "Компания MMI предоставляет своим клиентам лучшие условия для покупки современных гаджетов.&nbsp;</em></strong> Мы предлагаем только оригинальные и проверенные товары по самым низким ценам в городе. Лучшие продукты от Apple, HTC, Samsung, Blackberry и других производителей.",
                Url = "mmicentre"
            };
            var barModel = new TestModel
            {
                Id = Guid.NewGuid(),
                Title = "Samsung",
                Description =
                    "Samsung придерживается простой философии бизнеса: использовать имеющиеся таланты и технологии для производства совершенных продуктов и услуг, которые способны изменить мир к лучшему.",
                Url = "Samsung"
            };
            provider.AddModel(fooModel).AddModel(barModel);

            await searchProvider.AddOrUpdateEntitiesAsync(provider.Models.ToArray());
            await Task.Delay(TimeSpan.FromSeconds(5));
            var result = await searchProvider.SearchAsync("samsung", 10);
            Assert.Equal(provider.Models.Count, result.Length);
            Assert.Equal(barModel.Id, result.First().Id);
        }
    }

    public class ElasticSearchTestScope : BaseTestScope
    {
        protected override TestApplication ConfigureApplication(TestApplication application, string name)
        {
            return base.ConfigureApplication(application, name)
                .AddModule<TestApplication, ElasticSearchModule, ElasticSearchModuleConfig>(
                    (configuration, _, moduleConfig) =>
                    {
                        moduleConfig.Url = configuration["ELASTICSEARCH_URL"];
                        moduleConfig.Prefix = name.ToLower();
                        moduleConfig.EnableClientLogging = true;
                    })
                .ConfigureServices(collection =>
                {
                    collection.AddSingleton<TestModelProvider>();
                    collection.RegisterSearchProvider<TestSearchProvider, TestModel, Guid>();
                });
        }
    }

    public class TestModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTimeOffset Date { get; set; } = DateTimeOffset.UtcNow;
    }

    public class TestSearchProvider : BaseSearchProvider<TestModel, Guid, BaseSearchModel>
    {
        private readonly TestModelProvider _testModelProvider;

        public TestSearchProvider(ILogger<TestSearchProvider> logger,
            TestModelProvider testModelProvider,
            ISearcher<BaseSearchModel>? searcher = null) : base(logger, searcher)
        {
            _testModelProvider = testModelProvider;
        }

        protected override Guid ParseId(string id)
        {
            return Guid.Parse(id);
        }

        protected override Task<BaseSearchModel[]> GetSearchModelsAsync(TestModel[] entities,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(entities
                .Select(e => new BaseSearchModel(e.Id.ToString(), e.Title, e.Url, e.Description, e.Date)).ToArray());
        }

        protected override Task<TestModel[]> GetEntitiesAsync(BaseSearchModel[] searchModels,
            CancellationToken cancellationToken = default)
        {
            var ids = searchModels.Select(m => Guid.Parse(m.Id));
            return Task.FromResult(_testModelProvider.Models.Where(m => ids.Contains(m.Id)).ToArray());
        }

        protected override string GetId(TestModel entity)
        {
            return entity.Id.ToString();
        }
    }

    public class TestModelProvider
    {
        public readonly List<TestModel> Models = new List<TestModel>();

        public TestModelProvider AddModel(TestModel model)
        {
            if (Models.All(m => m.Id != model.Id))
            {
                Models.Add(model);
            }

            return this;
        }
    }
}
