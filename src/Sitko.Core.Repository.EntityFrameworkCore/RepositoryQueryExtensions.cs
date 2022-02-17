using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Sitko.Core.Repository.EntityFrameworkCore;

[PublicAPI]
public static class RepositoryQueryExtensions
{
    public static async Task<(T[] items, int itemsCount)> GetAllAsync<T>(this EFRepositoryQuery<T> query)
        where T : class
    {
        var dbQuery = query.BuildQuery();
        var needCount = false;
        if (query.Offset != null)
        {
            dbQuery = dbQuery.Skip(query.Offset.Value);
            needCount = true;
        }

        if (query.Limit != null)
        {
            dbQuery = dbQuery.Take(query.Limit.Value);
            needCount = true;
        }

        var items = await dbQuery.ToArrayAsync();
        var itemsCount = needCount && (query.Offset > 0 || items.Length == query.Limit)
            ? await query.BuildQuery().CountAsync()
            : items.Length;

        return (items, itemsCount);
    }
}
