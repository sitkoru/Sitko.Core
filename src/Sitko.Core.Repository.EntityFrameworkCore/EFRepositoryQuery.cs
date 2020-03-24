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
    public class EFRepositoryQuery<TEntity> : BaseRepositoryQuery<TEntity> where TEntity : class
    {
        protected IQueryable<TEntity> Query;

        protected readonly List<Func<IQueryable<TEntity>, IQueryable<TEntity>>> WhereExpressions =
            new List<Func<IQueryable<TEntity>, IQueryable<TEntity>>>();

        protected readonly List<(Expression<Func<TEntity, object>> expression, bool desc)> OrderByExpressions =
            new List<(Expression<Func<TEntity, object>> expression, bool desc)>();

        public EFRepositoryQuery(IQueryable<TEntity> query)
        {
            Query = query;
        }

        internal EFRepositoryQuery(IQueryable<TEntity> query,
            List<Func<IQueryable<TEntity>, IQueryable<TEntity>>> whereExpressions,
            List<(Expression<Func<TEntity, object>> expression, bool desc)> orderByExpressions) : this(query)
        {
            WhereExpressions = whereExpressions;
            OrderByExpressions = orderByExpressions;
        }

        public IQueryable<TEntity> BuildQuery()
        {
            foreach (var func in WhereExpressions)
            {
                Query = func.Invoke(Query);
            }

            foreach (var orderBy in OrderByExpressions)
            {
                Query = orderBy.desc
                    ? Query.OrderByDescending(orderBy.expression)
                    : Query.OrderBy(orderBy.expression);
            }

            return Query;
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
            Query = configureQuery(Query);
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
            return new IncludableRepositoryQuery<TEntity, TProperty>(Query.Include(navigationPropertyPath),
                WhereExpressions,
                OrderByExpressions);
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

    internal class IncludableRepositoryQuery<TEntity, TProperty> : EFRepositoryQuery<TEntity>,
        IIncludableRepositoryQuery<TEntity, TProperty> where TEntity : class
    {
        internal IncludableRepositoryQuery(IIncludableQueryable<TEntity, TProperty> query,
            List<Func<IQueryable<TEntity>, IQueryable<TEntity>>> whereExpressions,
            List<(Expression<Func<TEntity, object>> expression, bool desc)> orderByExpressions) : base(query,
            whereExpressions,
            orderByExpressions)
        {
        }

        public IIncludableRepositoryQuery<TEntity, TNextProperty> ThenInclude<TNextProperty>(
            Expression<Func<TProperty, TNextProperty>> navigationPropertyPath)
        {
            return new IncludableRepositoryQuery<TEntity, TNextProperty>(
                ((IIncludableQueryable<TEntity, TProperty>) Query).ThenInclude(navigationPropertyPath),
                WhereExpressions,
                OrderByExpressions);
        }
    }
}
