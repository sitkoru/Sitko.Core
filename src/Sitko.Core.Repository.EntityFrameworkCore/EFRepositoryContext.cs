using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using KellermanSoftware.CompareNetObjects;
using Sitko.Core.App.Compare;

namespace Sitko.Core.Repository.EntityFrameworkCore
{
    public class EFRepositoryContext<TEntity, TEntityPk, TDbContext> : IRepositoryContext<TEntity, TEntityPk>
        where TEntity : class, IEntity<TEntityPk> where TDbContext : DbContext
    {
        private readonly ILoggerFactory loggerFactory;
        private readonly IServiceProvider serviceProvider;

        public EFRepositoryContext(IServiceProvider serviceProvider, TDbContext dbContext,
            RepositoryFiltersManager filtersManager,
            ILoggerFactory loggerFactory,
            EFRepositoryLock repositoryLock,
            IEnumerable<IValidator>? validators = null,
            IEnumerable<IAccessChecker<TEntity, TEntityPk>>? accessCheckers = null,
            IEnumerable<ICompareLogicConfigurator>? compareLogicConfigurators = null)
        {
            this.serviceProvider = serviceProvider;
            this.loggerFactory = loggerFactory;
            DbContext = dbContext;
            FiltersManager = filtersManager;
            RepositoryLock = repositoryLock;
            Validators = validators?.ToList();
            AccessCheckers = accessCheckers?.ToList();
            ComparerConfigurators =
                compareLogicConfigurators?.ToList() ?? new List<ICompareLogicConfigurator>();
            ComparerConfigurators.Add(new EFRepositoryComparerConfigurator<TDbContext>(dbContext));
        }

        internal TDbContext DbContext { get; }
        public EFRepositoryLock RepositoryLock { get; }

        public ILogger<IRepository<TEntity, TEntityPk>> Logger =>
            loggerFactory.CreateLogger<EFRepository<TEntity, TEntityPk, TDbContext>>();

        public List<ICompareLogicConfigurator> ComparerConfigurators { get; }

        public RepositoryFiltersManager FiltersManager { get; }
        public List<IValidator>? Validators { get; }
        public List<IAccessChecker<TEntity, TEntityPk>>? AccessCheckers { get; }
    }

    public class EFRepositoryComparerConfigurator<TDbContext> : ICompareLogicConfigurator
        where TDbContext : DbContext
    {
        private readonly TDbContext dbContext;

        public EFRepositoryComparerConfigurator(TDbContext dbContext) => this.dbContext = dbContext;

        public void Configure(ComparisonConfig config)
        {
            foreach (var entityType in dbContext.Model.GetEntityTypes())
            {
                if (typeof(IEntity).IsAssignableFrom(entityType.ClrType))
                {
                    config.CollectionMatchingSpec.Add(entityType.ClrType, new[] { "Id" });
                }
            }
        }
    }
}
