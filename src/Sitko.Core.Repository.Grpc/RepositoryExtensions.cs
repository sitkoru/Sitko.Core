using System.Text;
using System.Web;
using Sitko.Core.Grpc;

namespace Sitko.Core.Repository.Grpc;

public static class RepositoryExtensions
{
    public static IRepositoryQuery<TEntity> ApplyRequestParams<TEntity>(this ApiRequestInfo requestParams,
        IRepositoryQuery<TEntity> query) where TEntity : class, IEntity
    {
        if (requestParams.Limit > 0)
        {
            query = query.Take(requestParams.Limit);
        }

        if (requestParams.Offset > 0)
        {
            query = query.Skip(requestParams.Offset);
        }

        if (!string.IsNullOrEmpty(requestParams.OrderBy))
        {
            query = query.OrderByString(requestParams.OrderBy);
        }

        if (string.IsNullOrEmpty(requestParams.Filter) || requestParams.Filter == "null")
        {
            return query;
        }

        var filter = requestParams.Filter;
        var num = filter.Length % 4;
        if (num > 0)
        {
            filter += new string('=', 4 - num);
        }

        var whereJson = HttpUtility.UrlDecode(Encoding.UTF8.GetString(Convert.FromBase64String(filter)));
        if (!string.IsNullOrEmpty(whereJson))
        {
            query = query.WhereByString(whereJson);
        }

        return query;
    }
}
