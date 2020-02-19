using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Sitko.Core.Repository
{
    public abstract class BaseRepositoryQuery<TEntity> : IRepositoryQuery<TEntity> where TEntity : class
    {
        public int? Limit { get; protected set; }
        public int? Offset { get; protected set; }

        public virtual IRepositoryQuery<TEntity> Take(int take)
        {
            Limit = take;
            return this;
        }

        public virtual IRepositoryQuery<TEntity> Skip(int skip)
        {
            Offset = skip;
            return this;
        }

        public abstract IRepositoryQuery<TEntity> Where(Expression<Func<TEntity, bool>> @where);
        public abstract IRepositoryQuery<TEntity> Where(string whereStr, object[] values);
        public abstract IRepositoryQuery<TEntity> OrderByDescending(Expression<Func<TEntity, object>> orderBy);
        public abstract IRepositoryQuery<TEntity> OrderBy(Expression<Func<TEntity, object>> orderBy);

        public abstract IRepositoryQuery<TEntity> Configure(Action<IRepositoryQuery<TEntity>>? configureQuery = null);

        public abstract Task<IRepositoryQuery<TEntity>> ConfigureAsync(
            Func<IRepositoryQuery<TEntity>, Task>? configureQuery = null);

        public virtual IRepositoryQuery<TEntity> OrderByString(string orderBy)
        {
            var sortQueries = GetSortParameters<TEntity>(orderBy);
            if (sortQueries.Any())
            {
                foreach (var sortQuery in sortQueries)
                {
                    ApplySort(sortQuery);
                }
            }

            return this;
        }

        public IRepositoryQuery<TEntity> OrderBy(string property, bool isDescending)
        {
            ApplySort((property, isDescending));
            return this;
        }

        protected abstract void ApplySort((string propertyName, bool isDescending) sortQuery);

        public virtual IRepositoryQuery<TEntity> Where(string property, object value)
        {
            SetCondition(property, QueryContextOperator.Equal, value);
            return this;
        }

        public virtual IRepositoryQuery<TEntity> Where(QueryContextCondition condition)
        {
            Where(new QueryContextConditionsGroup(new List<QueryContextCondition> {condition}));
            return this;
        }

        public IRepositoryQuery<TEntity> Where(QueryContextConditionsGroup conditionsGroup)
        {
            Where(new[] {conditionsGroup});
            return this;
        }

        public IRepositoryQuery<TEntity> Where(IEnumerable<QueryContextConditionsGroup> conditionsGroups)
        {
            var whereQueries = new List<string>();
            var valueIndex = 0;
            var values = new List<object?>();
            foreach (var conditionsGroup in conditionsGroups)
            {
                var groupWhere = new List<string>();
                foreach (var condition in conditionsGroup.Conditions)
                {
                    PrepareCondition(condition);
                    var expression = condition.GetExpression(valueIndex);
                    if (!string.IsNullOrEmpty(expression))
                    {
                        groupWhere.Add(expression);
                        values.Add(condition.Value);
                        valueIndex++;
                    }
                }

                whereQueries.Add($"({string.Join(" OR ", groupWhere)})");
            }

            var whereStr = string.Join(" AND ", whereQueries);
            Where(whereStr, values.ToArray());

            return this;
        }

        public virtual IRepositoryQuery<TEntity> Like(string property, object value)
        {
            SetCondition(property, QueryContextOperator.Contains, value);
            return this;
        }

        public virtual IRepositoryQuery<TEntity> WhereByString(string whereJson)
        {
            var where = JsonConvert.DeserializeObject<List<QueryContextConditionsGroup>>(whereJson);
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (where != null)
            {
                var conditionsGroups = new List<QueryContextConditionsGroup>();
                foreach (var conditionsGroup in where)
                {
                    var group = new QueryContextConditionsGroup(new List<QueryContextCondition>());
                    foreach (var parsedCondition in conditionsGroup.Conditions
                        .Select(condition => CreateCondition(condition.Property, condition.Operator, condition.Value))
                        .Where(parsedCondition => parsedCondition != null))
                    {
                        group.Conditions.Add(parsedCondition);
                    }

                    if (group.Conditions.Any())
                    {
                        conditionsGroups.Add(group);
                    }
                }

                if (conditionsGroups.Any())
                {
                    Where(conditionsGroups);
                }
            }

            return this;
        }

        protected QueryContextCondition? CreateCondition(string property, QueryContextOperator @operator, object value)
        {
            var propertyInfo = FieldsResolver.GetPropertyInfo<TEntity>(property);
            if (propertyInfo != null)
            {
                var condition = new QueryContextCondition
                {
                    Property = propertyInfo.Value.name, Operator = @operator
                };
                if (value != null)
                {
                    condition.Value = ParsePropertyValue(condition.ValueType, condition.Value);
                }

                return condition;
            }

            return null;
        }

        protected void PrepareCondition(QueryContextCondition condition)
        {
            var propertyInfo = FieldsResolver.GetPropertyInfo<TEntity>(condition.Property);
            if (propertyInfo != null)
            {
                condition.Property = propertyInfo.Value.name;
                condition.ValueType = propertyInfo.Value.type;
                if (condition.Value != null)
                {
                    //condition.Value = ParsePropertyValue(condition.ValueType, condition.Value);
                }
            }
        }

        protected virtual void SetCondition(string property, QueryContextOperator @operator, object value)
        {
            var condition = CreateCondition(property, @operator, value);
            if (condition != null)
            {
                Where(condition);
            }
        }

        public virtual IRepositoryQuery<TEntity> Paginate(int page, int itemsPerPage)
        {
            var offset = 0;
            if (page > 0)
            {
                offset = (page - 1) * itemsPerPage;
            }

            Offset = offset;
            Limit = itemsPerPage;
            return this;
        }

        private static object? ParsePropertyValue(Type propertyType, object? value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is JArray arr)
            {
                var values = Activator.CreateInstance(typeof(List<>).MakeGenericType(propertyType)) as IList;
                if (values != null)
                {
                    foreach (var child in arr.Children())
                    {
                        values.Add(ParsePropertyValue(propertyType, child));
                    }
                }

                return values;
            }

            object? parsedValue = null;
            var nullableType = Nullable.GetUnderlyingType(propertyType);
            if (nullableType != null)
            {
                propertyType = nullableType;
            }

            if (propertyType.IsEnum)
            {
                var enumType = propertyType;
                var parsed = int.TryParse(value.ToString(), out var intValue);

                if (Enum.IsDefined(enumType, value.ToString()) || parsed && Enum.IsDefined(enumType, intValue))
                    parsedValue = Enum.Parse(enumType, value.ToString());
            }

            else if (propertyType == typeof(bool))
            {
                parsedValue = value.ToString() == "1" ||
                              value.ToString() == "true" ||
                              value.ToString() == "on" ||
                              value.ToString() == "checked";
            }
            else if (propertyType == typeof(Uri))
            {
                parsedValue = new Uri(Convert.ToString(value));
            }
            else if (propertyType == typeof(DateTimeOffset) || propertyType == typeof(DateTimeOffset?))
            {
                if (DateTimeOffset.TryParse(value.ToString(), out var dto))
                {
                    parsedValue = dto;
                }
            }
            else if (propertyType == typeof(Guid))
            {
                if (Guid.TryParse(value.ToString(), out var dto))
                {
                    parsedValue = dto;
                }
            }
            else
            {
                parsedValue = Convert.ChangeType(value.ToString(), propertyType);
            }

            return parsedValue;
        }

        protected static List<(string propertyName, bool isDescending)> GetSortParameters<T>(string orderBy)
        {
            var sortParameters = new List<(string propertyName, bool isDescending)>();
            if (!string.IsNullOrEmpty(orderBy))
            {
                orderBy.Split(',').ToList().ForEach(p =>
                {
                    var isDescending = false;
                    if (p[0] == '-')
                    {
                        isDescending = true;
                        p = p.Substring(1);
                    }

                    var propertyName = FieldsResolver.GetPropertyInfo<T>(p);
                    if (propertyName.HasValue)
                    {
                        sortParameters.Add((propertyName.Value.name, isDescending));
                    }
                });
            }

            return sortParameters;
        }
    }
}
