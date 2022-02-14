using System.Linq.Expressions;
using Newtonsoft.Json;
using Serialize.Linq.Serializers;
using JsonSerializer = Serialize.Linq.Serializers.JsonSerializer;

namespace Sitko.Core.Repository.Remote;

public record SerializedQuery<TEntity> where TEntity : class
{
    private ExpressionSerializer _serializer = new ExpressionSerializer(new JsonSerializer());

    [JsonConstructor]
    public SerializedQuery()
    {

    }

    public SerializedQuery(List<Expression<Func<TEntity, bool>>> @where,
        List<Expression<Func<TEntity, object>>> orderBy,
        List<Expression<Func<TEntity, object>>> orderByDescending,
        List<Expression<Func<TEntity, int>>> intSelectExpressions,
        List<Expression<Func<TEntity, long>>> longSelectExpressions,
        List<string> includes,
        int? limit, int? offset)
    {
        Where = where.Select(node => _serializer.SerializeText(node)).ToList();
        OrderBy = orderBy.Select(node => _serializer.SerializeText(node)).ToList();
        OrderByDescending = orderByDescending.Select(node => _serializer.SerializeText(node)).ToList();
        IntSelectExpressions = intSelectExpressions.Select(node => _serializer.SerializeText(node)).ToList();
        LongSelectExpressions = longSelectExpressions.Select(node => _serializer.SerializeText(node)).ToList();
        Limit = limit;
        Offset = offset;
    }

    public IRepositoryQuery<TEntity> Apply(IRepositoryQuery<TEntity> query)
    {
        foreach (var expressionNode in Where)
        {
            var ex = _serializer.DeserializeText(expressionNode);
            query = query.Where((Expression<Func<TEntity, bool>>)ex);
        }

        foreach (var expressionNode in OrderBy)
        {
            var ex = _serializer.DeserializeText(expressionNode);
            query = query.OrderBy((Expression<Func<TEntity, object>>)ex);
        }

        foreach (var expressionNode in OrderByDescending)
        {
            var ex = _serializer.DeserializeText(expressionNode);
            query = query.OrderByDescending((Expression<Func<TEntity, object>>)ex);
        }

        foreach (var expressionNode in IntSelectExpressions)
        {
            var ex = _serializer.DeserializeText(expressionNode);
            query = query.Select((Expression<Func<TEntity, int>>)ex);
        }

        foreach (var expressionNode in LongSelectExpressions)
        {
            var ex = _serializer.DeserializeText(expressionNode);
            query = query.Select((Expression<Func<TEntity, long>>)ex);
        }

        foreach (var expressionNode in Includes)
        {
            var ex = _serializer.DeserializeText(expressionNode);
            query = query.Include((Expression<Func<TEntity, object>>)ex);
        }

        if (Limit > 0)
        {
            query = query.Take(Limit.Value);
        }

        if (Offset > 0)
        {
            query = query.Skip(Offset.Value);
        }

        return query;
    }

    public List<string> Where { get; set; }
    public List<string> OrderBy { get; set; }
    public List<string> OrderByDescending { get; set; }
    List<string> IntSelectExpressions { get; set; }
    List<string> LongSelectExpressions { get; set; }
    List<string> Includes { get; set; }
    public int? Limit { get; set; }
    public int? Offset { get; set; }
}
