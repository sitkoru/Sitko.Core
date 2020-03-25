using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Sitko.Core.Repository.EntityFrameworkCore
{
    internal class EFRepositoryQuerySource<TEntity> where TEntity : class
    {
        internal IQueryable<TEntity> Query;

        public EFRepositoryQuerySource(IQueryable<TEntity> query)
        {
            Query = query;
        }
    }

    public class EFRepositoryQuery<TEntity> : BaseRepositoryQuery<TEntity> where TEntity : class
    {
        internal EFRepositoryQuerySource<TEntity> QuerySource;

        internal readonly List<Func<IQueryable<TEntity>, IQueryable<TEntity>>> WhereExpressions =
            new List<Func<IQueryable<TEntity>, IQueryable<TEntity>>>();

        internal readonly List<(Expression<Func<TEntity, object>> expression, bool desc)> OrderByExpressions =
            new List<(Expression<Func<TEntity, object>> expression, bool desc)>();

        public EFRepositoryQuery(IQueryable<TEntity> query)
        {
            QuerySource = new EFRepositoryQuerySource<TEntity>(query);
        }

        internal EFRepositoryQuery(EFRepositoryQuerySource<TEntity> source,
            List<Func<IQueryable<TEntity>, IQueryable<TEntity>>> whereExpressions,
            List<(Expression<Func<TEntity, object>> expression, bool desc)> orderByExpressions)
        {
            QuerySource = source;
            WhereExpressions = whereExpressions;
            OrderByExpressions = orderByExpressions;
        }

        public IQueryable<TEntity> BuildQuery()
        {
            foreach (var func in WhereExpressions)
            {
                QuerySource.Query = func.Invoke(QuerySource.Query);
            }

            foreach (var orderBy in OrderByExpressions)
            {
                QuerySource.Query = orderBy.desc
                    ? QuerySource.Query.OrderByDescending(orderBy.expression)
                    : QuerySource.Query.OrderBy(orderBy.expression);
            }

            return QuerySource.Query;
        }


        public override IRepositoryQuery<TEntity> Where(Expression<Func<TEntity, bool>> where)
        {
            WhereExpressions.Add(query => query.Where(where));
            return this;
        }

        public override IRepositoryQuery<TEntity> Where(string whereStr, object?[] values)
        {
            WhereExpressions.Add(query => query.Where(whereStr, values));
            return this;
        }

        public override IRepositoryQuery<TEntity> OrderByDescending(Expression<Func<TEntity, object>> orderBy)
        {
            OrderByExpressions.Add((orderBy, true));
            return this;
        }

        public override IRepositoryQuery<TEntity> OrderBy(Expression<Func<TEntity, object>> orderBy)
        {
            OrderByExpressions.Add((orderBy, false));
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
            Func<IRepositoryQuery<TEntity>, Task>? configureQuery = null)
        {
            if (configureQuery != null)
            {
                await configureQuery(this);
            }

            return this;
        }

        public override IIncludableRepositoryQuery<TEntity, TProperty> Include<TProperty>(
            Expression<Func<TEntity, TProperty>> navigationPropertyPath)
        {
            QuerySource.Query = QuerySource.Query.Include(navigationPropertyPath);
            var query = new EFIncludableRepositoryQuery<TEntity, TProperty>(QuerySource,
                WhereExpressions,
                OrderByExpressions);
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
            List<(Expression<Func<TEntity, object>> expression, bool desc)> orderByExpressions) : base(source,
            whereExpressions,
            orderByExpressions)
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
                efQuery.OrderByExpressions);
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
                efQuery.OrderByExpressions);
            return query;
        }
    }

    public static class IIncludableRepositoryQueryExtensions
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
