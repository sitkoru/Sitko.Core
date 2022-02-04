using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Sitko.Core.Repository.Remote.Server;

public class BaseRemoteRepositoryController<TEntity, TEntityPK> : Controller where TEntity : class, IEntity<TEntityPK>
{
    private readonly IRepository<TEntity, TEntityPK> repository;

    public BaseRemoteRepositoryController(IRepository<TEntity, TEntityPK> repository) => this.repository = repository;


    // [HttpGet("GetAll")]
    // public async Task<string> GetAllAsync()
    // {
    //     var result = await repository.GetAllAsync();
    //     return JsonSerializer.Serialize(result);
    // }

    [HttpGet("GetAll")]
    public async Task<string> GetAllAsync(string json)
    {
        var query = JsonSerializer.Deserialize<SerializedQuery<TEntity>>(json);
        var result = await repository.GetAllAsync(repositoryQuery => query.Apply(repositoryQuery));
        return JsonSerializer.Serialize(result);
    }

    [HttpGet("Get")]
    public async Task<string> GetAsync(string json)
    {
        var query = JsonSerializer.Deserialize<SerializedQuery<TEntity>>(json);
        var result = await repository.GetAsync(repositoryQuery => query.Apply(repositoryQuery));
        return JsonSerializer.Serialize(result);
    }

    [HttpGet("GetById")]
    public async Task<TEntity?> GetByIdAsync(TEntityPK key)
    {
        return await repository.GetByIdAsync(key);
    }

    [HttpPost("Add")]
    public async Task<string> AddAsync(string json)
    {
        var entity = JsonSerializer.Deserialize<TEntity>(json);
        var result = repository.AddAsync(entity);
        return JsonSerializer.Serialize(result);
    }

    [HttpPost("AddRange")]
    public async Task<string> AddRangeAsync(string json)
    {
        var entity = JsonSerializer.Deserialize<TEntity[]>(json);
        var result = repository.AddAsync(entity);
        return JsonSerializer.Serialize(result);
    }

    [HttpPost("Update")]
    public async Task<string> UpdateAsync(string json)
    {
        var entity = JsonSerializer.Deserialize<UpdateModel<TEntity>>(json);
        var result = await repository.UpdateAsync(entity.Entity, entity.OldEntity);
        return JsonSerializer.Serialize(result);
    }

    [HttpGet("Count")]
    public async Task<int> CountAsync()
    {
        return await repository.CountAsync();
    }

    [HttpGet("Count")]
    public async Task<int> CountAsync(SerializedQuery<TEntity> query)
    {
        return await repository.CountAsync(q=>query.Apply(q));
    }

    [HttpGet("Sum")]
    public async Task<int> SumAsync(SerializedQuery<TEntity> query)
    {
        return await repository.SumAsync(q=>query.Apply(q), null);
    }

    [HttpDelete]
    public async Task<bool> DeleteAsync(TEntityPK key)
    {
        return await repository.DeleteAsync(key);
    }
}
