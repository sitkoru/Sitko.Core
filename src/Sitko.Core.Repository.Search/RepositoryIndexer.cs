using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Repository.Search
{
    public class RepositoryIndexer
    {
        private readonly ILogger<RepositoryIndexer> _logger;
        private readonly IEnumerable<IRepositorySearchProvider>? _repositorySearchProviders;

        public RepositoryIndexer(ILogger<RepositoryIndexer> logger,
            IEnumerable<IRepositorySearchProvider>? repositorySearchProviders = null)
        {
            _logger = logger;
            _repositorySearchProviders = repositorySearchProviders;
        }

        public async Task ReindexAllAsync(int batchSize = 1000, CancellationToken cancellationToken = default)
        {
            if (_repositorySearchProviders != null)
            {
                foreach (var searchProvider in _repositorySearchProviders)
                {
                    try
                    {
                        await searchProvider.ReindexAsync(batchSize, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reindexing {Provider}: {ErrorText}", searchProvider,
                            ex.ToString());
                    }
                }
            }
        }

        public async Task ReindexAsync<TEntity>(int batchSize = 1000, CancellationToken cancellationToken = default)
            where TEntity : class, IEntity
        {
            if (_repositorySearchProviders != null)
            {
                foreach (var searchProvider in _repositorySearchProviders.OfType<IRepositorySearchProvider<TEntity>>())
                {
                    try
                    {
                        await searchProvider.ReindexAsync(batchSize, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reindexing {Provider}: {ErrorText}", searchProvider,
                            ex.ToString());
                    }
                }
            }
        }
    }
}
