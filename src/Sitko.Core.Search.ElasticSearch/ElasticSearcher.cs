using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;

namespace Sitko.Core.Search.ElasticSearch;

public class ElasticSearcher<TSearchModel> : ISearcher<TSearchModel> where TSearchModel : BaseSearchModel
{
    private readonly ILogger<ElasticSearcher<TSearchModel>> logger;
    private readonly IOptionsMonitor<ElasticSearchModuleOptions> optionsMonitor;
    private ElasticClient? client;

    public ElasticSearcher(IOptionsMonitor<ElasticSearchModuleOptions> optionsMonitor,
        ILogger<ElasticSearcher<TSearchModel>> logger)
    {
        this.optionsMonitor = optionsMonitor;
        this.logger = logger;
    }

    private ElasticSearchModuleOptions Options => optionsMonitor.CurrentValue;

    public async Task<bool> AddOrUpdateAsync(string indexName, IEnumerable<TSearchModel> searchModels,
        CancellationToken cancellationToken = default)
    {
        indexName = $"{Options.Prefix}_{indexName}";
        var result = await GetClient().IndexManyAsync(searchModels, indexName.ToLowerInvariant(),
            cancellationToken);
        if (result.Errors)
        {
            foreach (var item in result.ItemsWithErrors)
            {
                logger.LogError("Error while indexing document {IndexName} {Id}: {ErrorText}", indexName, item.Id,
                    item.Error);
            }
        }

        if (result.ServerError != null)
        {
            logger.LogError("Error while indexing {IndexName} documents: {ErrorText}", indexName,
                result.ServerError);
        }

        return result.ApiCall.Success;
    }

    public async Task<bool> DeleteAsync(string indexName, IEnumerable<TSearchModel> searchModels,
        CancellationToken cancellationToken = default)
    {
        indexName = $"{Options.Prefix}_{indexName}";
        var result = await GetClient().DeleteManyAsync(searchModels, indexName.ToLowerInvariant(),
            cancellationToken);
        if (result.Errors)
        {
            foreach (var item in result.ItemsWithErrors)
            {
                logger.LogError("Error while deleting document {Id} from {IndexName}: {ErrorText}", item.Id,
                    indexName,
                    item.Error);
            }
        }

        if (result.ServerError != null)
        {
            logger.LogError("Error while deleting documents from {IndexName}: {ErrorText}", indexName,
                result.ServerError);
        }

        return !result.Errors;
    }

    public async Task<bool> DeleteAsync(string indexName, CancellationToken cancellationToken = default)
    {
        indexName = $"{Options.Prefix}_{indexName}";
        var result = await GetClient()
            .Indices.DeleteAsync(Indices.All, descriptor => descriptor.Index(indexName.ToLowerInvariant()),
                cancellationToken);
        if (result.ServerError != null)
        {
            logger.LogError("Error while deleting documents from {IndexName}: {ErrorText}", indexName,
                result.ServerError);
        }

        return result.Acknowledged;
    }

    public async Task<long> CountAsync(string indexName, string term, SearchOptions? searchOptions,
        CancellationToken cancellationToken = default)
    {
        indexName = $"{Options.Prefix}_{indexName}";
        var names = GetSearchText(term);
        var resultsCount = await GetClient().CountAsync<TSearchModel>(x =>
            x.Query(q =>
                    q.QueryString(qs => qs.Query(names)))
                .Index(indexName.ToLowerInvariant()), cancellationToken);
        if (resultsCount.ServerError != null)
        {
            logger.LogError("Error while counting documents in {IndexName}: {ErrorText}", indexName,
                resultsCount.ServerError);
        }

        return resultsCount.Count;
    }

    public async Task<SearcherEntity<TSearchModel>[]> SearchAsync(string indexName, string term,
        SearchOptions? searchOptions,
        CancellationToken cancellationToken = default)
    {
        indexName = $"{Options.Prefix}_{indexName}";
        searchOptions ??= new SearchOptions();
        var results = await GetClient()
            .SearchAsync<TSearchModel>(x => GetSearchRequest(x, indexName, term, searchOptions),
                cancellationToken);
        if (results.ServerError != null)
        {
            logger.LogError("Error while searching in {IndexName}: {ErrorText}", indexName, results.ServerError);
        }

        return results.Documents.Select(x =>
            new SearcherEntity<TSearchModel>(x, new Dictionary<string, IReadOnlyCollection<string>>())).ToArray();
    }

    public async Task<SearcherEntity<TSearchModel>[]> GetSimilarAsync(string indexName, string id,
        SearchOptions? searchOptions,
        CancellationToken cancellationToken = default)
    {
        indexName = $"{Options.Prefix}_{indexName}";
        searchOptions ??= new SearchOptions();
        var results = await GetClient()
            .SearchAsync<TSearchModel>(x => x.Query(q =>
                    q.MoreLikeThis(qs => qs.Like(descriptor =>
                            descriptor.Document(documentDescriptor => documentDescriptor.Index(indexName).Id(id)))
                        .MinDocumentFrequency(1)
                        .MinTermFrequency(1)
                        .MaxQueryTerms(12)))
                .Sort(s => s.Descending(SortSpecialField.Score).Descending(model => model.Date))
                .Skip(searchOptions.Offset)
                .Take(searchOptions.Limit)
                .Index(indexName.ToLowerInvariant()), cancellationToken);
        if (results.ServerError != null)
        {
            logger.LogError("Error while looking for similar documents in {IndexName}: {ErrorText}", indexName,
                results.ServerError);
        }

        return results.Documents.Select(x =>
            new SearcherEntity<TSearchModel>(x, new Dictionary<string, IReadOnlyCollection<string>>())).ToArray();
    }

    public async Task InitAsync(string indexName, CancellationToken cancellationToken = default)
    {
        indexName = $"{Options.Prefix}_{indexName}";
        var indexExists = await GetClient().Indices.ExistsAsync(indexName, ct: cancellationToken);
        if (indexExists.Exists)
        {
            logger.LogDebug("Update existing index {IndexName}", indexName);
            await GetClient().Indices.CloseAsync(indexName, ct: cancellationToken);
            var result = await GetClient().Indices.UpdateSettingsAsync(indexName, c => c.IndexSettings(s =>
                s.Analysis(BuildIndexDescriptor)), cancellationToken);
            await GetClient().Indices.OpenAsync(indexName, ct: cancellationToken);
            if (!result.IsValid)
            {
                throw result.OriginalException;
            }
        }
        else
        {
            logger.LogDebug("Create new index {IndexName}", indexName);
            var result = await GetClient()
                .Indices.CreateAsync(indexName,
                    c => c.Settings(s => s.Analysis(BuildIndexDescriptor)), cancellationToken);
            if (!result.IsValid)
            {
                throw result.OriginalException;
            }
        }
    }

    private ElasticClient GetClient()
    {
        if (client == null)
        {
            logger.LogDebug("Create elastic client");
            var settings = new ConnectionSettings(new Uri(Options.Url)).DisableDirectStreaming()
                .OnRequestCompleted(details =>
                {
                    if (Options.EnableClientLogging)
                    {
                        logger.LogDebug("### ES REQEUST ###");
                        if (details.RequestBodyInBytes != null)
                        {
                            logger.LogDebug("{Request}", Encoding.UTF8.GetString(details.RequestBodyInBytes));
                        }

                        logger.LogDebug("### ES RESPONSE ###");
                        if (details.ResponseBodyInBytes != null)
                        {
                            logger.LogDebug("{Response}", Encoding.UTF8.GetString(details.ResponseBodyInBytes));
                        }
                    }
                })
                .PrettyJson();
            if (!string.IsNullOrEmpty(Options.Login))
            {
                settings.BasicAuthentication(Options.Login, Options.Password);
            }

            settings.ServerCertificateValidationCallback((_, _, _, _) => true);
            client = new ElasticClient(settings);
        }

        return client;
    }

    private static SearchDescriptor<TSearchModel> GetSearchRequest(SearchDescriptor<TSearchModel> descriptor,
        string indexName, string term,
        SearchOptions searchOptions)
    {
        var names = GetSearchText(term);

        return descriptor.Query(q =>
                q.QueryString(qs =>
                    qs.Query(names)))
            .Sort(s => s.Descending(SortSpecialField.Score).Descending(model => model.Date))
            .Skip(searchOptions.Offset)
            .Take(searchOptions.Limit)
            .Index(indexName.ToLowerInvariant());
    }

    private static string GetSearchText(string? term)
    {
        var names = "";
        if (term != null)
        {
            names = term.Replace("+", " OR ");
        }

        return names;
    }

    private AnalysisDescriptor BuildIndexDescriptor(AnalysisDescriptor a) =>
        a
            .Analyzers(aa => aa
                .Custom("default",
                    descriptor =>
                        descriptor.Tokenizer("standard")
                            .CharFilters("html_strip")
                            .Filters("lowercase", "ru_RU", "en_US"))
            ).TokenFilters(descriptor =>
                descriptor.Hunspell("ru_RU", hh => hh.Dedup().Locale("ru_RU"))
                    .Hunspell("en_US", hh => hh.Dedup().Locale("en_US")));
}
