namespace Sitko.Core.Search;

public record SearchResult<TEntity, TSearchModel>
{
    public TEntity Entity { get; set; }
    public TSearchModel ResultModel { get; set; }
}
