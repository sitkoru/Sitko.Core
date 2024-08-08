using System.Globalization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Search.OpenSearch.Tests;

public class OpenSearchTests(ITestOutputHelper testOutputHelper) : BaseTest<OpenSearchTestScope>(testOutputHelper)
{
    [Fact]
    public async Task SearchAsync()
    {
        var scope = await GetScopeAsync();
        var provider = scope.GetService<TestModelProvider>();
        var searchProvider = scope.GetService<ISearchProvider<TestModel, Guid, TestSearchModel>>();
        await searchProvider.DeleteIndexAsync();
        await searchProvider.InitAsync();

        var fooModel = new TestModel
        {
            Title = "MMI",
            Description =
                "Компания MMI предоставляет своим клиентам лучшие условия для покупки современных гаджетов.&nbsp;</em></strong> Мы предлагаем только оригинальные и проверенные товары по самым низким ценам в городе. Лучшие продукты от Apple, HTC, Samsung, Blackberry и других производителей.",
            Url = "mmicentre"
        };
        var barModel = new TestModel
        {
            Title = "Samsung",
            Description =
                "Samsung придерживается простой философии бизнеса: использовать имеющиеся таланты и технологии для производства совершенных продуктов и услуг, которые способны изменить мир к лучшему.",
            Url = "Samsung"
        };
        provider.AddModel(fooModel).AddModel(barModel);

        await searchProvider.AddOrUpdateEntitiesAsync(provider.Models.ToArray());
        await Task.Delay(TimeSpan.FromSeconds(5));
        var result = await searchProvider.SearchAsync("samsung", 10, SearchType.Morphology);
        result.Length.Should().Be(provider.Models.Count);
    }

    [Theory(DisplayName = "MorphologyRusTest")]
    [InlineData(1, "Геймеры")]
    [InlineData(1, "игра")]
    [InlineData(1, "играть")]
    [InlineData(2, "компьютерный")]
    [InlineData(1, "геймер")]
    public async Task MorphologyRusTestAsync(int foundDocs, string searchText)
    {
        var scope = await GetScopeAsync();
        var provider = scope.GetService<TestModelProvider>();
        var searchProvider = scope.GetService<ISearchProvider<TestModel, Guid, TestSearchModel>>();
        await searchProvider.DeleteIndexAsync();
        await searchProvider.InitAsync();

        var firstModel = new TestModel
        {
            Title = "MMI", Description = "Геймеры играют в компьютерные игры.", Url = "mmicentre"
        };
        var secondModel = new TestModel { Title = "MMI", Description = "компьютерный", Url = "mmicentre" };
        var thirdModel = new TestModel { Title = "MMI", Description = "ГГ", Url = "mmicentre" };
        var forthModel = new TestModel { Title = "MMI", Description = "MMI", Url = "mmicentre" };
        provider.AddModel(firstModel).AddModel(secondModel).AddModel(thirdModel).AddModel(forthModel);

        await searchProvider.AddOrUpdateEntitiesAsync(provider.Models.ToArray());
        await Task.Delay(TimeSpan.FromSeconds(5));

        var result = await searchProvider.SearchAsync(searchText, 10, SearchType.Morphology);
        result.Length.Should().Be(foundDocs);
    }

    [Fact]
    public async Task MorphologyEngTestAsync()
    {
        var scope = await GetScopeAsync();
        var provider = scope.GetService<TestModelProvider>();
        var searchProvider = scope.GetService<ISearchProvider<TestModel, Guid, TestSearchModel>>();
        await searchProvider.DeleteIndexAsync();
        await searchProvider.InitAsync();

        var firstModel = new TestModel { Title = "MMI", Description = "Walk", Url = "mmicentre" };
        var secondModel = new TestModel { Title = "MMI", Description = "walked", Url = "mmicentre" };
        var thirdModel =
            new TestModel { Title = "MMI", Description = "walking", Url = "mmicentre" };
        var forthModel = new TestModel { Title = "MMI", Description = "MMI", Url = "mmicentre" };
        provider.AddModel(firstModel).AddModel(secondModel).AddModel(thirdModel).AddModel(forthModel);

        await searchProvider.AddOrUpdateEntitiesAsync(provider.Models.ToArray());
        await Task.Delay(TimeSpan.FromSeconds(5));

        var result = await searchProvider.SearchAsync("walked", 10, SearchType.Morphology);
        result.Length.Should().Be(3);
    }

    [Theory(DisplayName = "PartialSearchEngTest")]
    [InlineData(1, "74")]
    [InlineData(1, "kol")]
    [InlineData(1, "kolesa")]
    [InlineData(1, "74ko")]
    public async Task PartialSearchEngTestAsync(int foundDocs, string searchText)
    {
        var scope = await GetScopeAsync();
        var provider = scope.GetService<TestModelProvider>();
        var searchProvider = scope.GetService<ISearchProvider<TestModel, Guid, TestSearchModel>>();
        await searchProvider.DeleteIndexAsync();
        await searchProvider.InitAsync();

        var firstModel = new TestModel { Title = "MMI", Description = "74kolesa", Url = "mmicentre" };
        var secondModel = new TestModel { Title = "MMI", Description = "walked", Url = "mmicentre" };
        var thirdModel =
            new TestModel { Title = "MMI", Description = "walking", Url = "mmicentre" };
        var forthModel = new TestModel { Title = "MMI", Description = "MMI", Url = "mmicentre" };
        provider.AddModel(firstModel).AddModel(secondModel).AddModel(thirdModel).AddModel(forthModel);

        await searchProvider.AddOrUpdateEntitiesAsync(provider.Models.ToArray());
        await Task.Delay(TimeSpan.FromSeconds(5));

        var result = await searchProvider.SearchAsync(searchText, 10, SearchType.Wildcard);
        result.Length.Should().Be(foundDocs);
    }

    [Theory(DisplayName = "PartialSearchRusTest")]
    [InlineData(1, "кол")]
    [InlineData(1, "74кол")]
    [InlineData(1, "74колес")]
    [InlineData(1, "74ко")]
    public async Task PartialSearchRusTestAsync(int foundDocs, string searchText)
    {
        var scope = await GetScopeAsync();
        var provider = scope.GetService<TestModelProvider>();
        var searchProvider = scope.GetService<ISearchProvider<TestModel, Guid, TestSearchModel>>();
        await searchProvider.DeleteIndexAsync();
        await searchProvider.InitAsync();

        var firstModel = new TestModel { Title = "MMI", Description = "74колеса", Url = "mmicentre" };
        var secondModel = new TestModel { Title = "MMI", Description = "walked", Url = "mmicentre" };
        var thirdModel =
            new TestModel { Title = "MMI", Description = "walking", Url = "mmicentre" };
        var forthModel = new TestModel { Title = "MMI", Description = "MMI", Url = "mmicentre" };
        provider.AddModel(firstModel).AddModel(secondModel).AddModel(thirdModel).AddModel(forthModel);

        await searchProvider.AddOrUpdateEntitiesAsync(provider.Models.ToArray());
        await Task.Delay(TimeSpan.FromSeconds(5));

        var result = await searchProvider.SearchAsync(searchText, 10, SearchType.Wildcard);
        result.Length.Should().Be(foundDocs);
    }

    [Theory(DisplayName = "SearchByNumbersTest")]
    [InlineData(2, "74", SearchType.Morphology)]
    [InlineData(2, "74", SearchType.Wildcard)]
    public async Task SearchByNumbersTestAsync(int foundDocs, string searchText, SearchType searchType)
    {
        var scope = await GetScopeAsync();
        var provider = scope.GetService<TestModelProvider>();
        var searchProvider = scope.GetService<ISearchProvider<TestModel, Guid, TestSearchModel>>();
        await searchProvider.DeleteIndexAsync();
        await searchProvider.InitAsync();
        var firstGuid = Guid.Parse("dd134352-da92-4cd2-9c" + searchText + "-440be713aba5");
        var secondGuid = Guid.Parse("dd134352-da92-4cd2-9c88-440be713aba5");
        var thirdGuid = Guid.Parse("dd134352-da92-4cd3-9c88-440be713aba5");

        var firstModel = new TestModel { Id = firstGuid, Title = "MMI", Description = "MMI", Url = $"/page/{firstGuid.ToString()}" };
        var secondModel = new TestModel { Id = secondGuid, Title = "MMI", Description = searchText, Url = "mmicentre" };
        var thirdModel = new TestModel { Id = thirdGuid, Title = searchText, Description = "MMI", Url = "mmicentre" };
        provider.AddModel(firstModel).AddModel(secondModel).AddModel(thirdModel);

        await searchProvider.AddOrUpdateEntitiesAsync(provider.Models.ToArray());
        await Task.Delay(TimeSpan.FromSeconds(5));

        var result2 = await searchProvider.SearchAsync(searchText, 10, searchType);
        result2.Length.Should().Be(foundDocs);
    }

    [Fact]
    public async Task IncorrectLayoutKeyboardTestAsync()
    {
        var scope = await GetScopeAsync();
        var provider = scope.GetService<TestModelProvider>();
        var searchProvider = scope.GetService<ISearchProvider<TestModel, Guid, TestSearchModel>>();
        await searchProvider.DeleteIndexAsync();
        await searchProvider.InitAsync();

        var firstModel = new TestModel { Title = "kolesa", Description = "MMI", Url = "/page/" };
        var secondModel = new TestModel { Title = "MMI", Description = "mmicentre", Url = "mmicentre" };
        provider.AddModel(firstModel).AddModel(secondModel);

        await searchProvider.AddOrUpdateEntitiesAsync(provider.Models.ToArray());
        await Task.Delay(TimeSpan.FromSeconds(5));

        var result = await searchProvider.SearchAsync("лщдуыф", 10, SearchType.Wildcard);
        result.Length.Should().Be(1);
    }

    [Fact]
    public async Task HighlightingTestAsync()
    {
        var scope = await GetScopeAsync();
        var provider = scope.GetService<TestModelProvider>();
        var searchProvider = scope.GetService<ISearchProvider<TestModel, Guid, TestSearchModel>>();
        await searchProvider.DeleteIndexAsync();
        await searchProvider.InitAsync();

        var firstModel = new TestModel
        {
            Title = "Геймеры играют в компьютерные игры.", Description = "MMI", Url = "mmicentre"
        };
        var secondModel = new TestModel { Title = "MMI", Description = "mmicentre", Url = "mmicentre" };
        provider.AddModel(firstModel).AddModel(secondModel);

        await searchProvider.AddOrUpdateEntitiesAsync(provider.Models.ToArray());
        await Task.Delay(TimeSpan.FromSeconds(5));

        var result = await searchProvider.SearchAsync("играют", 10, SearchType.Morphology, true);
        result.Length.Should().Be(1);
        result.First().ResultModel.Highlight.Count.Should().Be(1);
        result.First().ResultModel.Highlight.First().Value.Contains("<span class='highlight'>");
    }
}

public class OpenSearchTestScope : BaseTestScope
{
    protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name)
    {
        base.ConfigureApplication(hostBuilder, name);
        hostBuilder.AddOpenSearch(moduleOptions =>
        {
            moduleOptions.Prefix = name.ToLower(CultureInfo.InvariantCulture);
            moduleOptions.EnableClientLogging = true;
            moduleOptions.Url = hostBuilder.Configuration.GetSection("OpenSearchModuleOptions")["Url"];
            moduleOptions.Login = hostBuilder.Configuration.GetSection("OpenSearchModuleOptions")["Login"];
            moduleOptions.Password = hostBuilder.Configuration.GetSection("OpenSearchModuleOptions")["Password"];
            moduleOptions.InitProviders = false;
            moduleOptions.DisableCertificatesValidation = true;
            moduleOptions.CustomStemmer = "russian";
            moduleOptions.PreTags = "<span class='highlight'>";
            moduleOptions.PreTags = "</span>";
        });

        hostBuilder.Services.AddSingleton<TestModelProvider>();
        hostBuilder.Services.RegisterSearchProvider<TestSearchProvider, TestModel, Guid, TestSearchModel>();
        return hostBuilder;
    }
}

public class TestModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset Date { get; set; } = DateTimeOffset.UtcNow;
}

public class TestSearchModel : BaseSearchModel
{
    public TestSearchModel()
    {
    }
}

public class TestSearchProvider(
    ILogger<TestSearchProvider> logger,
    TestModelProvider testModelProvider,
    ISearcher<TestSearchModel>? searcher = null)
    : BaseSearchProvider<TestModel, Guid, TestSearchModel>(logger, searcher)
{
    protected override Guid ParseId(string id) => Guid.Parse(id);

    protected override Task<TestSearchModel[]> GetSearchModelsAsync(TestModel[] entities,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(entities
            .Select(e => new TestSearchModel
            {
                Id = e.Id.ToString(),
                Date = e.Date,
                Url = e.Url,
                Title = e.Title,
                Content = e.Description
            })
            .ToArray());

    protected override Task<SearchResult<TestModel, TestSearchModel>[]> GetEntitiesAsync(TestSearchModel[] searchModels,
        CancellationToken cancellationToken = default)
    {
        var ids = searchModels.Select(m => Guid.Parse(m.Id));
        var entities = testModelProvider.Models.Where(m => ids.Contains(m.Id));
        List<SearchResult<TestModel, TestSearchModel>> result = [];
        foreach (var entity in entities)
        {
            var searchModel = searchModels.ToList().FirstOrDefault(model => model.Id == entity.Id.ToString());
            if (searchModel != null)
            {
                result.Add(new SearchResult<TestModel, TestSearchModel> { Entity = entity, ResultModel = searchModel });
            }
        }

        return Task.FromResult(result.ToArray());
    }

    protected override string GetId(TestModel entity) => entity.Id.ToString();
}

public class TestModelProvider
{
    public List<TestModel> Models { get; } = new();

    public TestModelProvider AddModel(TestModel model)
    {
        if (Models.All(m => m.Id != model.Id))
        {
            Models.Add(model);
        }

        return this;
    }
}
