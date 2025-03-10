using System.Globalization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenSearch.Net;
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

        var result = await searchProvider.SearchAsync("samsung");
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

        var result = await searchProvider.SearchAsync(searchText);
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

        var result = await searchProvider.SearchAsync("walked");
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

        var result =
            await searchProvider.SearchAsync(searchText, new SearchOptions { SearchType = SearchType.Wildcard });
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

        var result =
            await searchProvider.SearchAsync(searchText, new SearchOptions { SearchType = SearchType.Wildcard });
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

        var firstModel = new TestModel
        {
            Id = firstGuid, Title = "MMI", Description = "MMI", Url = $"/page/{firstGuid.ToString()}"
        };
        var secondModel = new TestModel { Id = secondGuid, Title = "MMI", Description = searchText, Url = "mmicentre" };
        var thirdModel = new TestModel { Id = thirdGuid, Title = searchText, Description = "MMI", Url = "mmicentre" };
        provider.AddModel(firstModel).AddModel(secondModel).AddModel(thirdModel);

        await searchProvider.AddOrUpdateEntitiesAsync(provider.Models.ToArray());

        var result2 = await searchProvider.SearchAsync(searchText, new SearchOptions { SearchType = searchType });
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

        var result =
            await searchProvider.SearchAsync("лщдуыф", new SearchOptions { SearchType = SearchType.Wildcard });
        result.Length.Should().Be(1);
    }


    [Theory(DisplayName = "HighlightingTest")]
    [InlineData("играют", SearchType.Wildcard)]
    [InlineData("компьютерный", SearchType.Morphology)]
    public async Task HighlightingTestAsync(string searchText, SearchType searchType)
    {
        var scope = await GetScopeAsync();
        var provider = scope.GetService<TestModelProvider>();
        var searchProvider = scope.GetService<ISearchProvider<TestModel, Guid, TestSearchModel>>();
        await searchProvider.DeleteIndexAsync();
        await searchProvider.InitAsync();

        var firstModel = new TestModel
        {
            Title = "Геймеры играют в компьютерные игры.",
            Description = "Геймеры играют в компьютерные игры.",
            Url = "mmicentre"
        };
        var secondModel = new TestModel { Title = "MMI", Description = "mmicentre", Url = "mmicentre" };
        provider.AddModel(firstModel).AddModel(secondModel);

        await searchProvider.AddOrUpdateEntitiesAsync(provider.Models.ToArray());

        var result = await searchProvider.SearchAsync(searchText,
            new SearchOptions { SearchType = searchType, WithHighlight = true });
        result.Length.Should().Be(1);
        result.First().Highlight.Count.Should().Be(1);
        result.First().Highlight.First().Value.First().Contains("<span class='highlight'>").Should()
            .BeTrue();
        result.First().Highlight.First().Value.First().Contains("</span>").Should().BeTrue();
    }


    [Theory(DisplayName = "Search with tags")]
    [InlineData(new[] { "ProjectId1" }, 1, 1)]
    [InlineData(new[] { "ProjectId2" }, 1, 1)]
    [InlineData(new[] { "ProjectId1", "ProjectId2" }, 1, 2)]
    [InlineData(new[] { "ProjectId1", "ProjectId2" }, 2, 0)]
    [InlineData(new[] { "ProjectId1", "ProjectId2", "ProjectId3", "SomeOtherTag" }, 1, 2)]
    [InlineData(new[] { "ProjectId1", "ProjectId2", "ProjectId3", "SomeOtherTag" }, 2, 2)]
    [InlineData(new[] { "ProjectId1", "ProjectId2", "ProjectId3", "SomeOtherTag" }, 3, 0)]
    [InlineData(new[] { "ProjectId3" }, 1, 0)]
    [InlineData(new string[0], 1, 2)]
    public async Task TagsAsync(string[] tags, int minimalMatch, int expected)
    {
        var scope = await GetScopeAsync();
        var searchProvider = scope.GetService<ISearchProvider<TestModel, Guid, TestSearchModel>>();
        var provider = scope.GetService<TestModelProvider>();
        await searchProvider.DeleteIndexAsync();
        await searchProvider.InitAsync();
        var firstModel = new TestModel
        {
            Title = "Геймеры играют в компьютерные игры.",
            Description = "Геймеры играют в компьютерные игры.",
            Url = "mmicentre",
            ProjectId = 1
        };
        var secondModel = new TestModel
        {
            Title = "Геймеры играют в настольные игры.",
            Description = "Геймеры играют в настольные игры.",
            Url = "mmicentre",
            ProjectId = 2
        };
        provider.AddModel(firstModel).AddModel(secondModel);

        await searchProvider.AddOrUpdateEntitiesAsync(provider.Models.ToArray());

        var result = await searchProvider.SearchAsync("играют",
            new SearchOptions { Tags = tags, TagsMinimumMatch = minimalMatch });
        result.Length.Should().Be(expected);
    }

    [Theory(DisplayName = "Search with limit")]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 2)]
    public async Task LimitAsync(int limit, int expected)
    {
        var scope = await GetScopeAsync();
        var searchProvider = scope.GetService<ISearchProvider<TestModel, Guid, TestSearchModel>>();
        var provider = scope.GetService<TestModelProvider>();
        await searchProvider.DeleteIndexAsync();
        await searchProvider.InitAsync();

        var firstModel = new TestModel
        {
            Title = "Геймеры играют в компьютерные игры.",
            Description = "Геймеры играют в компьютерные игры.",
            Url = "mmicentre"
        };
        var secondModel = new TestModel
        {
            Title = "Геймеры играют в настольные игры.",
            Description = "Геймеры играют в настольные игры.",
            Url = "mmicentre"
        };

        provider.AddModel(firstModel).AddModel(secondModel);

        await searchProvider.AddOrUpdateEntitiesAsync(provider.Models.ToArray());

        var result = await searchProvider.SearchAsync("играют",
        new SearchOptions { Limit = limit });
        result.Length.Should().Be(expected);
    }

    [Theory(DisplayName = "Search with offset")]
    [InlineData(0, 2)]
    [InlineData(1, 1)]
    [InlineData(2, 0)]
    public async Task OffsetAsync(int offset, int expected)
    {
        var scope = await GetScopeAsync();
        var searchProvider = scope.GetService<ISearchProvider<TestModel, Guid, TestSearchModel>>();
        var provider = scope.GetService<TestModelProvider>();
        await searchProvider.DeleteIndexAsync();
        await searchProvider.InitAsync();

        var firstModel = new TestModel
        {
            Title = "Геймеры играют в компьютерные игры.",
            Description = "Геймеры играют в компьютерные игры.",
            Url = "mmicentre"
        };
        var secondModel = new TestModel
        {
            Title = "Геймеры играют в настольные игры.",
            Description = "Геймеры играют в настольные игры.",
            Url = "mmicentre"
        };

        provider.AddModel(firstModel).AddModel(secondModel);

        await searchProvider.AddOrUpdateEntitiesAsync(provider.Models.ToArray());

        var result = await searchProvider.SearchAsync("играют",
        new SearchOptions { Offset = offset });
        result.Length.Should().Be(expected);
    }

    [Theory(DisplayName = "Counting")]
    [InlineData("компьютерные", SearchType.Morphology, 1)]
    [InlineData("ге", SearchType.Wildcard, 2)]
    [InlineData("ге", SearchType.Morphology, 0)]
    [InlineData("", SearchType.Wildcard, 2)]
    [InlineData("mmicentre", SearchType.Morphology, 2)]
    public async Task CountAsync(string searchText, SearchType searchType, int expected)
    {
        var scope = await GetScopeAsync();
        var searchProvider = scope.GetService<ISearchProvider<TestModel, Guid, TestSearchModel>>();
        var provider = scope.GetService<TestModelProvider>();
        await searchProvider.DeleteIndexAsync();
        await searchProvider.InitAsync();

        var firstModel = new TestModel
        {
            Title = "Геймеры играют в компьютерные игры.",
            Description = "Геймеры играет в компьютерные игры.",
            Url = "mmicentre"
        };
        var secondModel = new TestModel
        {
            Title = "Геймер играют в настольные игры.",
            Description = "Геймеры играют в настольные игры.",
            Url = "mmicentre"
        };

        provider.AddModel(firstModel).AddModel(secondModel);

        await searchProvider.AddOrUpdateEntitiesAsync(provider.Models.ToArray());

        var result = await searchProvider.CountAsync(searchText,
        new SearchOptions { SearchType = searchType });
        result.Should().Be(expected);
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
            moduleOptions.InitProviders = false;
            moduleOptions.DisableCertificatesValidation = true;
            moduleOptions.CustomStemmer = "russian";
            moduleOptions.PreTags = "<span class='highlight'>";
            moduleOptions.PostTags = "</span>";
            moduleOptions.Refresh = Refresh.True; // Force new data propagation
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

    public int? ProjectId { get; set; }
}

public class TestSearchModel : BaseSearchModel
{
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
                Content = e.Description,
                Tags = [$"ProjectId{e.ProjectId}", "SomeOtherTag"]
            })
            .ToArray());

    protected override Task<SearchResult<TestModel>[]> GetEntitiesAsync(SearcherEntity<TestSearchModel>[] searchModels,
        CancellationToken cancellationToken = default)
    {
        var ids = searchModels.Select(m => Guid.Parse(m.SearchModel.Id));
        var entities = testModelProvider.Models.Where(m => ids.Contains(m.Id));
        List<SearchResult<TestModel>> result = [];
        foreach (var entity in entities)
        {
            var searcherResult = searchModels.ToList()
                .FirstOrDefault(model => model.SearchModel.Id == entity.Id.ToString());
            if (searcherResult != null)
            {
                result.Add(new SearchResult<TestModel>(entity, searcherResult.Highlight));
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
