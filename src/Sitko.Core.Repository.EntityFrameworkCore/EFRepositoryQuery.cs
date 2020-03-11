using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Sitko.Core.Repository.EntityFrameworkCore
{
    public class EFRepositoryQuery<TEntity> : BaseRepositoryQuery<TEntity> where TEntity : class
    {
        private IQueryable<TEntity> _query;

        private readonly List<Func<IQueryable<TEntity>, IQueryable<TEntity>>> _where =
            new List<Func<IQueryable<TEntity>, IQueryable<TEntity>>>();

        private readonly List<(Expression<Func<TEntity, object>> expression, bool desc)> _orderBy =
            new List<(Expression<Func<TEntity, object>> expression, bool desc)>();

        public EFRepositoryQuery(IQueryable<TEntity> query)
        {
            _query = query;
        }

        public IQueryable<TEntity> BuildQuery()
        {
            foreach (var func in _where)
            {
                _query = func.Invoke(_query);
            }

            foreach (var orderBy in _orderBy)
            {
                _query = orderBy.desc
                    ? _query.OrderByDescending(orderBy.expression)
                    : _query.OrderBy(orderBy.expression);
            }

            return _query;
        }


        public override IRepositoryQuery<TEntity> Where(Expression<Func<TEntity, bool>> where)
        {
            _where.Add(query => query.Where(where));
            return this;
        }

        public override IRepositoryQuery<TEntity> Where(string whereStr, object?[] values)
        {
            _where.Add(query => query.Where(whereStr, values));
            return this;
        }

        public override IRepositoryQuery<TEntity> OrderByDescending(Expression<Func<TEntity, object>> orderBy)
        {
            _orderBy.Add((orderBy, true));
            return this;
        }

        public override IRepositoryQuery<TEntity> OrderBy(Expression<Func<TEntity, object>> orderBy)
        {
            _orderBy.Add((orderBy, false));
            return this;
        }

        public IRepositoryQuery<TEntity> Configure(Func<IQueryable<TEntity>, IQueryable<TEntity>> configureQuery)
        {
            _query = configureQuery(_query);
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
}
