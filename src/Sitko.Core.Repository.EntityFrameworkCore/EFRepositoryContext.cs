using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
            IEnumerable<IValidator>? validators = null,
            IEnumerable<IAccessChecker<TEntity, TEntityPk>>? accessCheckers = null)
        {
            this.serviceProvider = serviceProvider;
            this.loggerFactory = loggerFactory;
            DbContext = dbContext;
            FiltersManager = filtersManager;
            Validators = validators?.ToList();
            AccessCheckers = accessCheckers?.ToList();
        }

        internal TDbContext DbContext { get; }

        public ILogger<IRepository<TEntity, TEntityPk>> Logger =>
            loggerFactory.CreateLogger<EFRepository<TEntity, TEntityPk, TDbContext>>();

        public RepositoryFiltersManager FiltersManager { get; }
        public List<IValidator>? Validators { get; }
        public List<IAccessChecker<TEntity, TEntityPk>>? AccessCheckers { get; }

        public IServiceScope CreateScope() => serviceProvider.CreateScope();
    }
}
