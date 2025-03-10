using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenSearch.Client;
using OpenSearch.Net;

namespace Sitko.Core.Search.OpenSearch;

public class OpenSearchSearcher<TSearchModel>(
    IOptionsMonitor<OpenSearchModuleOptions> optionsMonitor,
    ILogger<OpenSearchSearcher<TSearchModel>> logger)
    : ISearcher<TSearchModel>
    where TSearchModel : BaseSearchModel, new()
{
    private const string CustomAnalyze = "custom_analyze";
    private const string CustomCharFilterAnalyze = "char_filter_analyze";
    private const string StemmerName = "custom_stemmer";
    private const string CustomCharFilter = "rus_en_key";
    private OpenSearchClient? client;
    private OpenSearchModuleOptions Options => optionsMonitor.CurrentValue;

    public async Task<bool> AddOrUpdateAsync(string indexName, IEnumerable<TSearchModel> searchModels,
        CancellationToken cancellationToken = default)
    {
        indexName = $"{Options.Prefix}_{indexName}";

        var bulkRequest = new BulkRequest(indexName.ToLowerInvariant());
        var indexOps = searchModels
            .Select(o => new BulkIndexOperation<TSearchModel>(o))
            .Cast<IBulkOperation>()
            .ToList();
        bulkRequest.Operations = new BulkOperationsCollection<IBulkOperation>(indexOps);
        bulkRequest.Refresh = optionsMonitor.CurrentValue.Refresh;
        var result = await GetClient().BulkAsync(bulkRequest, cancellationToken);

        var hasErrors = result.ApiCall.Success;
        if (result.Errors)
        {
            foreach (var item in result.ItemsWithErrors)
            {
                logger.LogError("Error while indexing document {IndexName} {Id}: {ErrorText}", indexName, item.Id,
                    item.Error);
            }

            hasErrors = true;
        }

        if (result.ServerError != null)
        {
            logger.LogError("Error while indexing {IndexName} documents: {ErrorText}", indexName,
                result.ServerError);
            hasErrors = true;
        }

        return !hasErrors;
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
                    indexName, item.Error);
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
        searchOptions ??= new SearchOptions();
        var resultsCount = await GetClient().CountAsync<TSearchModel>(x =>
            x.Query(q =>
                {
                    q.QueryString(qs =>
                        searchOptions.SearchType == SearchType.Morphology
                        ? qs.Query(names)
                        : qs.Query($"*{names}*").AnalyzeWildcard());
                    return ApplyTagsFilter(q, searchOptions);
                })
                .Index(indexName.ToLowerInvariant()), cancellationToken);
        if (resultsCount.ServerError != null)
        {
            logger.LogError("Error while counting documents in {IndexName}: {ErrorText}", indexName,
                resultsCount.ServerError);
        }

        return resultsCount.Count;
    }

    public async Task<SearcherEntity<TSearchModel>[]> SearchAsync(string indexName, string term,
        SearchOptions? searchOptions, CancellationToken cancellationToken = default)
    {
        indexName = $"{Options.Prefix}_{indexName}";
        searchOptions ??= new SearchOptions();
        var searchResponse = await GetClient()
            .SearchAsync<TSearchModel>(
                x => GetSearchRequest(x, indexName, term, searchOptions),
                cancellationToken);
        if (searchResponse.ServerError != null)
        {
            logger.LogError("Error while searching in {IndexName}: {ErrorText}", indexName, searchResponse.ServerError);
        }

        var result = searchResponse.Hits.Select(h => new SearcherEntity<TSearchModel>(h.Source, h.Highlight)).ToArray();
        return result;
    }

    public async Task<SearcherEntity<TSearchModel>[]> GetSimilarAsync(string indexName, string id,
        SearchOptions? searchOptions,
        CancellationToken cancellationToken = default)
    {
        indexName = $"{Options.Prefix}_{indexName}";
        searchOptions ??= new SearchOptions();
        var results = await GetClient()
            .SearchAsync<TSearchModel>(x => x.Query(q =>
                {
                    q.MoreLikeThis(qs => qs.Like(descriptor =>
                            descriptor.Document(documentDescriptor => documentDescriptor.Index(indexName).Id(id)))
                        .MinDocumentFrequency(1)
                        .MinTermFrequency(1)
                        .MaxQueryTerms(12));
                    return ApplyTagsFilter(q, searchOptions);
                })
                .Sort(s => s.Descending(SortSpecialField.Score).Descending(model => model.Date))
                .Skip(searchOptions.Offset)
                .Take(searchOptions.Limit)
                .Index(indexName.ToLowerInvariant()), cancellationToken);
        if (results.ServerError != null)
        {
            logger.LogError("Error while looking for similar documents in {IndexName}: {ErrorText}", indexName,
                results.ServerError);
        }

        return results.Documents.Select(h =>
            new SearcherEntity<TSearchModel>(h, new Dictionary<string, IReadOnlyCollection<string>>())).ToArray();
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
                s.Analysis(CreateAnalysisDescriptor)), cancellationToken);
            await GetClient().Indices.OpenAsync(indexName, ct: cancellationToken);
            if (!result.IsValid)
            {
                if (result.ServerError != null)
                {
                    logger.LogError("Error while init {IndexName} index: {ErrorText}", indexName,
                        result.ServerError);
                    throw new InvalidOperationException("Error while init " + indexName + " index: " +
                                                        result.ServerError);
                }

                if (result.OriginalException != null)
                {
                    logger.LogError(result.OriginalException.Message);
                    throw result.OriginalException;
                }
            }
        }
        else
        {
            logger.LogDebug("Create new index {IndexName}", indexName);
            var result = await GetClient().Indices.CreateAsync(indexName, CreateIndexDescriptor, ct: cancellationToken);
            if (!result.IsValid)
            {
                if (result.ServerError != null)
                {
                    logger.LogError("Error while create {IndexName} index: {ErrorText}", indexName,
                        result.ServerError);
                    throw new InvalidOperationException("Error while init " + indexName + " index: " +
                                                        result.ServerError);
                }

                if (result.OriginalException != null)
                {
                    logger.LogError(result.OriginalException.Message);
                    throw result.OriginalException;
                }
            }
        }
    }

#pragma warning disable CA1859
    private static QueryContainer ApplyTagsFilter(QueryContainerDescriptor<TSearchModel> q,
#pragma warning restore CA1859
        SearchOptions? searchOptions)
    {
        if (searchOptions?.Tags.Length > 0)
        {
            return q && q
                .TermsSet(ts => ts
                    .Field(d => d.Tags)
                    .Terms(searchOptions.Tags)
                    .MinimumShouldMatchScript(sr => sr
                        .Source(searchOptions.TagsMinimumMatch.ToString(CultureInfo
                            .InvariantCulture))
                    )
                );
        }

        return q;
    }

    private OpenSearchClient GetClient()
    {
        if (client != null)
        {
            return client;
        }

        logger.LogDebug("Create OpenSearch client");
        var settings = new ConnectionSettings(new Uri(Options.Url)).DisableDirectStreaming()
            .OnRequestCompleted(details =>
            {
                if (!Options.EnableClientLogging)
                {
                    return;
                }

                logger.LogDebug("### OpenSearch REQUEST ###");
                if (details.RequestBodyInBytes != null)
                {
                    logger.LogDebug("{Request}", Encoding.UTF8.GetString(details.RequestBodyInBytes));
                }

                logger.LogDebug("### OpenSearch RESPONSE ###");
                if (details.ResponseBodyInBytes != null)
                {
                    logger.LogDebug("{Response}", Encoding.UTF8.GetString(details.ResponseBodyInBytes));
                }
            })
            .PrettyJson();
        if (!string.IsNullOrEmpty(Options.Login))
        {
            settings.BasicAuthentication(Options.Login, Options.Password);
        }

        if (Options.DisableCertificatesValidation)
        {
            settings.ServerCertificateValidationCallback(CertificateValidations.AllowAll)
                .ServerCertificateValidationCallback((_, _, _, _) => true);
        }

        client = new OpenSearchClient(settings);
        return client;
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

    private SearchDescriptor<TSearchModel> GetSearchRequest(SearchDescriptor<TSearchModel> descriptor,
        string indexName, string term, SearchOptions searchOptions)
    {
        var names = GetSearchText(term);
        descriptor.Query(q =>
        {
            q.QueryString(qs =>
                searchOptions.SearchType == SearchType.Morphology
                    ? qs.Fields(fieldsDescriptor => fieldsDescriptor.Field(searchModel => searchModel.Title)
                        .Field(searchModel => searchModel.Content)).Query(names)
                    : qs.Fields(fieldsDescriptor => fieldsDescriptor
                        .Field(searchModel => searchModel.Title)
                        .Field(searchModel => searchModel.Content)).Query($"*{names}*").AnalyzeWildcard());

            return ApplyTagsFilter(q, searchOptions);
        });

        if (searchOptions.WithHighlight)
        {
            descriptor.Highlight(h =>
                h.Fields(fs => fs
                        .Field(p => p.Title)
                        .PreTags(Options.PreTags)
                        .PostTags(Options.PostTags),
                    fs => fs
                        .Field(p => p.Content)
                        .PreTags(Options.PreTags)
                        .PostTags(Options.PostTags)));
        }

        return descriptor
            .Sort(s => s.Descending(SortSpecialField.Score).Descending(model => model.Date))
            .Skip(searchOptions.Offset)
            .Take(searchOptions.Limit)
            .Index(indexName.ToLowerInvariant());
    }

    private AnalysisDescriptor CreateAnalysisDescriptor(AnalysisDescriptor a) =>
        a.Analyzers(aa =>
                aa.Custom(CustomAnalyze, ca => ca
                        .Tokenizer("standard")
                        .Filters("lowercase", "stop", "snowball", StemmerName))
                    .Custom(CustomCharFilterAnalyze, ca => ca
                        .Tokenizer("standard")
                        .Filters("lowercase", "stop")
                        .CharFilters(CustomCharFilter))
            )
            .CharFilters(descriptor =>
                descriptor.Mapping(CustomCharFilter,
                    filterDescriptor => filterDescriptor.Mappings(OpenSearchHelper.RusEnKeys)))
            .TokenFilters(descriptor =>
                descriptor.Stemmer(StemmerName, filterDescriptor => filterDescriptor.Language(Options.CustomStemmer)));

    private CreateIndexDescriptor CreateIndexDescriptor(CreateIndexDescriptor createIndexDescriptor) =>
        createIndexDescriptor.Settings(s => s.Analysis(CreateAnalysisDescriptor))
            .Map<TSearchModel>(mm => mm
                .Properties(p => p
                    .Text(t => t
                        .Name(n => n.Content)
                        .Analyzer(CustomAnalyze)
                    )
                    .Text(t => t
                        .Name(n => n.Title)
                        .Analyzer(CustomCharFilterAnalyze)
                    )
                    .Keyword(t => t.Name(x => x.Tags))
                )
            );
}
