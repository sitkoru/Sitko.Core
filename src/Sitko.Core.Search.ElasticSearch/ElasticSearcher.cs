using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nest;

namespace Sitko.Core.Search.ElasticSearch
{
    public class ElasticSearcher<TSearchModel> : ISearcher<TSearchModel> where TSearchModel : BaseSearchModel
    {
        private readonly ILogger<ElasticSearcher<TSearchModel>> _logger;
        private readonly ElasticSearchModuleConfig _options;
        private ElasticClient? _client;

        public ElasticSearcher(ElasticSearchModuleConfig options, ILogger<ElasticSearcher<TSearchModel>> logger)
        {
            _logger = logger;
            _options = options;
        }

        private ElasticClient GetClient()
        {
            if (_client == null)
            {
                _logger.LogDebug("Create elastic client");
                var settings = new ConnectionSettings(new Uri(_options.Url)).DisableDirectStreaming()
                    .OnRequestCompleted(details =>
                    {
                        if (_options.EnableClientLogging)
                        {
                            _logger.LogDebug("### ES REQEUST ###");
                            if (details.RequestBodyInBytes != null)
                                _logger.LogDebug(Encoding.UTF8.GetString(details.RequestBodyInBytes));
                            _logger.LogDebug("### ES RESPONSE ###");
                            if (details.ResponseBodyInBytes != null)
                                _logger.LogDebug(Encoding.UTF8.GetString(details.ResponseBodyInBytes));
                        }
                    })
                    .PrettyJson();
                if (!string.IsNullOrEmpty(_options.Login))
                {
                    settings.BasicAuthentication(_options.Login, _options.Password);
                }

                settings.ServerCertificateValidationCallback((o, certificate, arg3, arg4) => true);
                _client = new ElasticClient(settings);
            }

            return _client;
        }

        private SearchDescriptor<TSearchModel> GetSearchRequest(SearchDescriptor<TSearchModel> descriptor,
            string indexName, string term,
            int limit = 0)
        {
            var names = GetSearchText(term);

            return descriptor.Query(q =>
                    q.QueryString(qs =>
                        qs.Query(names)))
                .Sort(s => s.Descending(SortSpecialField.Score).Descending(model => model.Date))
                .Size(limit > 0 ? limit : 20)
                .Index(indexName.ToLowerInvariant());
        }

        private static string GetSearchText(string term)
        {
            var names = "";
            if (term != null)
            {
                names = term.Replace("+", " OR ");
            }

            return names;
        }

        public async Task<bool> AddOrUpdateAsync(string indexName, IEnumerable<TSearchModel> searchModels)
        {
            indexName = $"{_options.Prefix}_{indexName}";
            var result = await GetClient().IndexManyAsync(searchModels, indexName.ToLowerInvariant());
            if (result.Errors)
            {
                foreach (var item in result.ItemsWithErrors)
                {
                    _logger.LogError("Error while indexing document {IndexName} {Id}: {ErrorText}", indexName, item.Id,
                        item.Error);
                }
            }

            if (result.ServerError != null)
            {
                _logger.LogError("Error while indexing {IndexName} documents: {ErrorText}", indexName,
                    result.ServerError);
            }

            return result.ApiCall.Success;
        }

        public async Task<bool> DeleteAsync(string indexName, IEnumerable<TSearchModel> searchModels)
        {
            indexName = $"{_options.Prefix}_{indexName}";
            var result = await GetClient().DeleteManyAsync(searchModels, indexName.ToLowerInvariant());
            if (result.Errors)
            {
                foreach (var item in result.ItemsWithErrors)
                {
                    _logger.LogError("Error while deleting document {Id} from {IndexName}: {ErrorText}", item.Id,
                        indexName,
                        item.Error);
                }
            }

            if (result.ServerError != null)
            {
                _logger.LogError("Error while deleting documents from {IndexName}: {ErrorText}", indexName,
                    result.ServerError);
            }

            return !result.Errors;
        }

        public async Task<bool> DeleteAsync(string indexName)
        {
            indexName = $"{_options.Prefix}_{indexName}";
            var result = await GetClient()
                .Indices.DeleteAsync(Indices.All, descriptor => descriptor.Index(indexName.ToLowerInvariant()));
            if (result.ServerError != null)
            {
                _logger.LogError("Error while deleting documents from {IndexName}: {ErrorText}", indexName,
                    result.ServerError);
            }

            return result.Acknowledged;
        }

        public async Task<long> CountAsync(string indexName, string term)
        {
            indexName = $"{_options.Prefix}_{indexName}";
            var names = GetSearchText(term);
            var resultsCount = await GetClient().CountAsync<TSearchModel>(x =>
                x.Query(q =>
                        q.QueryString(qs => qs.Query(names)))
                    .Index(indexName.ToLowerInvariant()));
            if (resultsCount.ServerError != null)
            {
                _logger.LogError("Error while counting documents in {IndexName}: {ErrorText}", indexName,
                    resultsCount.ServerError);
            }

            return resultsCount.Count;
        }

        public async Task<TSearchModel[]> SearchAsync(string indexName, string term, int limit)
        {
            indexName = $"{_options.Prefix}_{indexName}";
            var results = await GetClient()
                .SearchAsync<TSearchModel>(x => GetSearchRequest(x, indexName, term, limit));
            if (results.ServerError != null)
            {
                _logger.LogError("Error while searching in {IndexName}: {ErrorText}", indexName, results.ServerError);
            }

            return results.Documents.ToArray();
        }

        public async Task<TSearchModel[]> GetSimilarAsync(string indexName, string id, int limit)
        {
            indexName = $"{_options.Prefix}_{indexName}";
            var results = await GetClient()
                .SearchAsync<TSearchModel>(x => x.Query(q =>
                        q.MoreLikeThis(qs => qs.Like(descriptor =>
                                descriptor.Document(documentDescriptor => documentDescriptor.Index(indexName).Id(id)))
                            .MinDocumentFrequency(1)
                            .MinTermFrequency(1)
                            .MaxQueryTerms(12)))
                    .Sort(s => s.Descending(SortSpecialField.Score).Descending(model => model.Date))
                    .Size(limit > 0 ? limit : 20)
                    .Index(indexName.ToLowerInvariant()));
            if (results.ServerError != null)
            {
                _logger.LogError("Error while looking for similar documents in {IndexName}: {ErrorText}", indexName,
                    results.ServerError);
            }

            return results.Documents.ToArray();
        }

        public async Task InitAsync(string indexName)
        {
            indexName = $"{_options.Prefix}_{indexName}";
            var indexExists = await GetClient().Indices.ExistsAsync(indexName);
            if (indexExists.Exists)
            {
                _logger.LogDebug("Update existing index {indexName}", indexName);
                await GetClient().Indices.CloseAsync(indexName);
                var result = await GetClient().Indices.UpdateSettingsAsync(indexName, c => c.IndexSettings(s =>
                    s.Analysis(BuildIndexDescriptor)));
                await GetClient().Indices.OpenAsync(indexName);
                if (!result.IsValid)
                {
                    throw result.OriginalException;
                }
            }
            else
            {
                _logger.LogDebug("Create new index {indexName}", indexName);
                var result = await GetClient()
                    .Indices.CreateAsync(indexName,
                        c => c.Settings(s => s.Analysis(BuildIndexDescriptor)));
                if (!result.IsValid)
                {
                    throw result.OriginalException;
                }
            }
        }

        private AnalysisDescriptor BuildIndexDescriptor(AnalysisDescriptor a)
        {
            return
                a
                    .Analyzers(aa => aa
                        .Custom("default",
                            descriptor =>
                                descriptor.Tokenizer("standard")
                                    .CharFilters("html_strip")
                                    .Filters("lowercase", "ru_RU", "en_US"))
                    ).TokenFilters(descriptor =>
                        descriptor.Hunspell("ru_RU", hh => hh.Dedup().Locale("ru_RU"))
                            .Hunspell("en_US", hh => hh.Dedup().Locale("en_US")))
                ;
        }
    }
}
