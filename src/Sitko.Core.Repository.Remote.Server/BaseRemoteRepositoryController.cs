using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Sitko.Core.Repository.Remote.Server;


public class BaseRemoteRepositoryController<TEntity, TEntityPK> : Controller where TEntity : class, IEntity<TEntityPK> where TEntityPK: class
{
    protected readonly IRepository<TEntity, TEntityPK> repository;

    public BaseRemoteRepositoryController(IRepository<TEntity, TEntityPK> repository) => this.repository = repository;


    [HttpGet("list")]
    public async Task<(TEntity[] items, int itemsCount)> GetAllAsync(SerializedQuery<TEntity> query)
    {
        return await repository.GetAllAsync(repositoryQuery => query.Apply(repositoryQuery));
    }

    [HttpGet]
    public async Task<TEntity?> GetAsync(SerializedQuery<TEntity> query)
    {
        return await repository.GetAsync(repositoryQuery => query.Apply(repositoryQuery));
    }

    [HttpGet("{TEntityPK}")]
    public async Task<TEntity?> GetByIdAsync(TEntityPK key)
    {
        return await repository.GetByIdAsync(key);
    }

    [HttpPost]
    public async Task<AddOrUpdateOperationResult<TEntity, TEntityPK>> AddAsync(TEntity entity)
    {
        return await repository.AddAsync(entity);
    }

    [HttpPost("update")]
    public async Task<AddOrUpdateOperationResult<TEntity, TEntityPK>> UpdateAsync(TEntity entity)
    {
        return await repository.UpdateAsync(entity);
    }

    [HttpGet("count")]
    public async Task<int> CountAsync()
    {
        return await repository.CountAsync();
    }

    [HttpDelete]
    public async Task<bool> DeleteAsync(TEntityPK key)
    {
        return await repository.DeleteAsync(key);
    }
}
