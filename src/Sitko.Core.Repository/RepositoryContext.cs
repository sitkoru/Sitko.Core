using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Repository
{
    public class RepositoryContext<TEntity, TEntityPk, TDbContext> : IDisposable
        where TEntity : class, IEntity<TEntityPk>
        where TDbContext : DbContext
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IServiceScope _scope;

        public RepositoryContext(IServiceScopeFactory serviceScopeFactory, ILoggerFactory loggerFactory)
        {
            _scope = serviceScopeFactory.CreateScope();
            _loggerFactory = loggerFactory;
            DbContext = _scope.ServiceProvider.GetRequiredService<TDbContext>();
            FiltersManager = _scope.ServiceProvider.GetRequiredService<RepositoryFiltersManager>();
            Validators = _scope.ServiceProvider.GetServices<IValidator<TEntity>>()?.ToList() ??
                         new List<IValidator<TEntity>>();
            AccessCheckers = _scope.ServiceProvider.GetServices<IAccessChecker<TEntity, TEntityPk>>()?.ToList() ??
                             new List<IAccessChecker<TEntity, TEntityPk>>();
        }

        internal TDbContext DbContext { get; }

        public ILogger<Repository<TEntity, TEntityPk, TDbContext>> Logger =>
            _loggerFactory.CreateLogger<Repository<TEntity, TEntityPk, TDbContext>>();

        public RepositoryFiltersManager FiltersManager { get; }
        public List<IValidator<TEntity>> Validators { get; }
        public List<IAccessChecker<TEntity, TEntityPk>> AccessCheckers { get; }

        public void Dispose()
        {
            _scope.Dispose();
        }
    }
}
