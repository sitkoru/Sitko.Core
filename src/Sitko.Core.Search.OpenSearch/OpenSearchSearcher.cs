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
    where TSearchModel : BaseSearchModel
{
    private OpenSearchModuleOptions Options => optionsMonitor.CurrentValue;
    private OpenSearchClient? client;
    private const string CustomAnalyze = "custom_analyze";
    private const string StemmerName = "custom_stemmer";

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

    public async Task<long> CountAsync(string indexName, string term, CancellationToken cancellationToken = default)
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

    public async Task<TSearchModel[]> SearchAsync(string indexName, string term, int limit,
        CancellationToken cancellationToken = default)
    {
        indexName = $"{Options.Prefix}_{indexName}";
        var results = await GetClient()
            .SearchAsync<TSearchModel>(x => GetSearchRequest(x, indexName, term, limit), cancellationToken);
        if (results.ServerError != null)
        {
            logger.LogError("Error while searching in {IndexName}: {ErrorText}", indexName, results.ServerError);
        }

        return results.Documents.ToArray();
    }

    public async Task<TSearchModel[]> GetSimilarAsync(string indexName, string id, int limit,
        CancellationToken cancellationToken = default)
    {
        indexName = $"{Options.Prefix}_{indexName}";
        var results = await GetClient()
            .SearchAsync<TSearchModel>(x => x.Query(q =>
                    q.MoreLikeThis(qs => qs.Like(descriptor =>
                            descriptor.Document(documentDescriptor => documentDescriptor.Index(indexName).Id(id)))
                        .MinDocumentFrequency(1)
                        .MinTermFrequency(1)
                        .MaxQueryTerms(12)))
                .Sort(s => s.Descending(SortSpecialField.Score).Descending(model => model.Date))
                .Size(limit > 0 ? limit : 20)
                .Index(indexName.ToLowerInvariant()), cancellationToken);
        if (results.ServerError != null)
        {
            logger.LogError("Error while looking for similar documents in {IndexName}: {ErrorText}", indexName,
                results.ServerError);
        }

        return results.Documents.ToArray();
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
                logger.LogError(result.OriginalException?.Message);
                throw result.OriginalException;
            }
        }
        else
        {
            logger.LogDebug("Create new index {IndexName}", indexName);
            var result = await GetClient().Indices.CreateAsync(indexName, CreateIndexDescriptor, ct: cancellationToken);
            if (!result.IsValid)
            {
                logger.LogError(result.OriginalException?.Message);
                throw result.OriginalException;
            }
        }
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

    private static SearchDescriptor<TSearchModel> GetSearchRequest(SearchDescriptor<TSearchModel> descriptor,
        string indexName, string term, int limit = 0)
    {
        var names = GetSearchText(term);
        return descriptor.Query(q => q.QueryString(qs => qs.Query(names)))
            .Sort(s => s.Descending(SortSpecialField.Score).Descending(model => model.Date))
            .Size(limit > 0 ? limit : 20)
            .Index(indexName.ToLowerInvariant());
    }

    private AnalysisDescriptor CreateAnalysisDescriptor(AnalysisDescriptor a) =>
        a.Analyzers(aa => aa.Custom(CustomAnalyze, ca => ca
                .Tokenizer("standard")
                .Filters("lowercase", "stop", "snowball", StemmerName)
            )
        ).TokenFilters(descriptor =>
            descriptor.Stemmer(StemmerName, filterDescriptor => filterDescriptor.Language(Options.CustomStemmer)));

    private CreateIndexDescriptor CreateIndexDescriptor(CreateIndexDescriptor createIndexDescriptor) =>
        createIndexDescriptor.Settings(s => s.Analysis(CreateAnalysisDescriptor))
            .Map<TSearchModel>(mm => mm
                .Properties(p => p
                    .Text(t => t
                        .Name(n => n.Content)
                        .Analyzer(CustomAnalyze)
                    )
                )
            );
}
