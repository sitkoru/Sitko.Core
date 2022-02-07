using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Sitko.Core.Repository.Remote.Server;

public class BaseRemoteRepositoryController<TEntity, TEntityPK> : Controller where TEntity : class, IEntity<TEntityPK>
{
    private readonly IRepository<TEntity, TEntityPK> repository;

    public BaseRemoteRepositoryController(IRepository<TEntity, TEntityPK> repository) => this.repository = repository;


    [HttpPost("GetAll")]
    public async Task<string> GetAllAsync([FromBody] SerializedQuery<TEntity> query)
    {
        var result = await repository.GetAllAsync(repositoryQuery => query.Apply(repositoryQuery));
        return JsonSerializer.Serialize(result);
    }

    [HttpPost("Get")]
    public async Task<string> GetAsync([FromBody]SerializedQuery<TEntity> query)
    {
        var result = await repository.GetAsync(repositoryQuery => query.Apply(repositoryQuery));
        return JsonSerializer.Serialize(result);
    }

    [HttpPost("GetById")]
    public async Task<TEntity?> GetByIdAsync([FromBody] TEntityPK key)
    {
        return await repository.GetByIdAsync(key);
    }

    [HttpPost("Add")]
    public async Task<string> AddAsync([FromBody] TEntity entity)
    {
        var result = repository.AddAsync(entity);
        return JsonSerializer.Serialize(result);
    }

    [HttpPost("AddRange")]
    public async Task<string> AddRangeAsync([FromBody] TEntity[] entities)
    {
        var result = repository.AddAsync(entities);
        return JsonSerializer.Serialize(result);
    }

    [HttpPost("Update")]
    public async Task<string> UpdateAsync([FromBody] UpdateModel<TEntity> model)
    {
        var result = await repository.UpdateAsync(model.Entity, model.OldEntity);
        return JsonSerializer.Serialize(result);
    }

    [HttpPost("Count")]
    public async Task<int> CountAsync([FromBody]SerializedQuery<TEntity> query)
    {
        return await repository.CountAsync(q=>query.Apply(q));
    }

    [HttpPost("Sum")]
    public async Task<int> SumAsync([FromBody]SerializedQuery<TEntity> query)
    {
        return await repository.SumAsync(q=>query.Apply(q), null);
    }

    [HttpDelete]
    public async Task<bool> DeleteAsync(TEntityPK key)
    {
        return await repository.DeleteAsync(key);
    }
}
