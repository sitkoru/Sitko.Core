using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Sitko.Core.Repository.EntityFrameworkCore
{
    internal class EFRepositoryQuerySource<TEntity> where TEntity : class
    {
        public EFRepositoryQuerySource(IQueryable<TEntity> query) => Query = query;
        internal IQueryable<TEntity> Query { get; set; }
    }

    public class EFRepositoryQuery<TEntity> : BaseRepositoryQuery<TEntity> where TEntity : class
    {
        public EFRepositoryQuery(IQueryable<TEntity> query) => QuerySource = new EFRepositoryQuerySource<TEntity>(query);

        internal EFRepositoryQuery(EFRepositoryQuerySource<TEntity> source,
            List<Func<IQueryable<TEntity>, IQueryable<TEntity>>> whereExpressions,
            List<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>> orderExpressions)
        {
            QuerySource = source;
            WhereExpressions = whereExpressions;
            OrderExpressions = orderExpressions;
        }

        internal EFRepositoryQuerySource<TEntity> QuerySource { get; }

        internal List<Func<IQueryable<TEntity>, IQueryable<TEntity>>> WhereExpressions { get; } = new();

        internal List<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>> OrderExpressions { get; } = new();

        public IQueryable<TEntity> BuildQuery()
        {
            foreach (var func in WhereExpressions)
            {
                QuerySource.Query = func(QuerySource.Query);
            }

            foreach (var orderBy in OrderExpressions)
            {
                QuerySource.Query = orderBy(QuerySource.Query);
            }

            return QuerySource.Query;
        }


        public override IRepositoryQuery<TEntity> Where(Expression<Func<TEntity, bool>> where)
        {
            WhereExpressions.Add(query => query.Where(where));
            return this;
        }

        public override IRepositoryQuery<TEntity> Where(Func<IQueryable<TEntity>, IQueryable<TEntity>> where)
        {
            WhereExpressions.Add(where);
            return this;
        }

        public override IRepositoryQuery<TEntity> Where(string whereStr, object?[] values)
        {
            WhereExpressions.Add(query => query.Where(whereStr, values));
            return this;
        }

        public override IRepositoryQuery<TEntity> OrderByDescending(Expression<Func<TEntity, object>> orderBy)
        {
            OrderExpressions.Add(entities => entities.OrderByDescending(orderBy));
            return this;
        }

        public override IRepositoryQuery<TEntity> OrderBy(Expression<Func<TEntity, object>> orderBy)
        {
            OrderExpressions.Add(entities => entities.OrderBy(orderBy));
            return this;
        }

        public override IRepositoryQuery<TEntity> Order(Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> order)
        {
            OrderExpressions.Add(order);
            return this;
        }

        public IRepositoryQuery<TEntity> Configure(Func<IQueryable<TEntity>, IQueryable<TEntity>> configureQuery)
        {
            QuerySource.Query = configureQuery(QuerySource.Query);
            return this;
        }

        public override IRepositoryQuery<TEntity> Configure(Action<IRepositoryQuery<TEntity>>? configureQuery = null)
        {
            configureQuery?.Invoke(this);

            return this;
        }

        public override async Task<IRepositoryQuery<TEntity>> ConfigureAsync(
            Func<IRepositoryQuery<TEntity>, Task>? configureQuery = null, CancellationToken cancellationToken = default)
        {
            if (configureQuery != null)
            {
                await configureQuery(this);
            }

            cancellationToken.ThrowIfCancellationRequested();
            return this;
        }

        public override IIncludableRepositoryQuery<TEntity, TProperty> Include<TProperty>(
            Expression<Func<TEntity, TProperty>> navigationPropertyPath)
        {
            QuerySource.Query = QuerySource.Query.Include(navigationPropertyPath);
            var query = new EFIncludableRepositoryQuery<TEntity, TProperty>(QuerySource,
                WhereExpressions,
                OrderExpressions);
            return query;
        }

        protected override void ApplySort((string propertyName, bool isDescending) sortQuery)
        {
            var property = typeof(TEntity).GetProperty(sortQuery.propertyName);
            if (property == null || !property.CanWrite)
            {
                return;
            }

            if (sortQuery.isDescending)
            {
                OrderByDescending(e => EF.Property<TEntity>(e, sortQuery.propertyName));
            }
            else
            {
                OrderBy(e => EF.Property<TEntity>(e, sortQuery.propertyName));
            }
        }
    }

    internal class EFIncludableRepositoryQuery<TEntity, TProperty> : EFRepositoryQuery<TEntity>,
        IIncludableRepositoryQuery<TEntity, TProperty> where TEntity : class
    {
        internal EFIncludableRepositoryQuery(EFRepositoryQuerySource<TEntity> source,
            List<Func<IQueryable<TEntity>, IQueryable<TEntity>>> whereExpressions,
            List<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>> orderExpressions) : base(source,
            whereExpressions,
            orderExpressions)
        {
        }

        internal static EFIncludableRepositoryQuery<TEntity, TNextProperty> ThenInclude<TPreviousProperty,
            TNextProperty>(
            IIncludableRepositoryQuery<TEntity, IEnumerable<TPreviousProperty>> source,
            Expression<Func<TPreviousProperty, TNextProperty>> navigationPropertyPath)
        {
            var efQuery = (EFRepositoryQuery<TEntity>)source;
            var querySource = (IIncludableQueryable<TEntity, IEnumerable<TPreviousProperty>>)efQuery.QuerySource.Query;
            efQuery.QuerySource.Query = querySource.ThenInclude(navigationPropertyPath);
            var query = new EFIncludableRepositoryQuery<TEntity, TNextProperty>(
                efQuery.QuerySource,
                efQuery.WhereExpressions,
                efQuery.OrderExpressions);
            return query;
        }

        internal static EFIncludableRepositoryQuery<TEntity, TNextProperty> ThenInclude<TPreviousProperty,
            TNextProperty>(
            IIncludableRepositoryQuery<TEntity, TPreviousProperty> source,
            Expression<Func<TPreviousProperty, TNextProperty>> navigationPropertyPath)
        {
            var efQuery = (EFRepositoryQuery<TEntity>)source;
            var querySource = (IIncludableQueryable<TEntity, TPreviousProperty>)efQuery.QuerySource.Query;
            efQuery.QuerySource.Query = querySource.ThenInclude(navigationPropertyPath);
            var query = new EFIncludableRepositoryQuery<TEntity, TNextProperty>(
                efQuery.QuerySource,
                efQuery.WhereExpressions,
                efQuery.OrderExpressions);
            return query;
        }
    }

    public static class IncludableRepositoryQueryExtensions
    {
        public static IIncludableRepositoryQuery<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
            this IIncludableRepositoryQuery<TEntity, IEnumerable<TPreviousProperty>> source,
            Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath)
            where TEntity : class
            => EFIncludableRepositoryQuery<TEntity, TProperty>.ThenInclude(source, navigationPropertyPath);

        public static IIncludableRepositoryQuery<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
            this IIncludableRepositoryQuery<TEntity, TPreviousProperty> source,
            Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath)
            where TEntity : class
            => EFIncludableRepositoryQuery<TEntity, TProperty>.ThenInclude(source, navigationPropertyPath);
    }
}
