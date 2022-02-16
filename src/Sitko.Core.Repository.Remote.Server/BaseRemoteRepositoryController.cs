using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sitko.Core.App.Json;

namespace Sitko.Core.Repository.Remote.Server;

public class BaseRemoteRepositoryController<TEntity, TEntityPK> : Controller where TEntity : class, IEntity<TEntityPK>
{
    private readonly IRepository<TEntity, TEntityPK> repository;

    public BaseRemoteRepositoryController(IRepository<TEntity, TEntityPK> repository) => this.repository = repository;


    [HttpPost("GetAll")]
    public async Task<IActionResult> GetAllAsync([FromBody] SerializedQueryData queryData)
    {
        try
        {
            var result = await repository.GetAllAsync(repositoryQuery =>
                new SerializedQuery<TEntity>(queryData).Apply(repositoryQuery));
            return Ok(JsonHelper.SerializeWithMetadata(new ListResult<TEntity>(result.items, result.itemsCount)));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("Get")]
    public async Task<IActionResult> GetAsync([FromBody] SerializedQueryData queryData)
    {
        var result = await repository.GetAsync(repositoryQuery =>
            new SerializedQuery<TEntity>(queryData).Apply(repositoryQuery));
        if (result is null)
        {
            return NotFound();
        }

        return Ok(JsonHelper.SerializeWithMetadata(result));
    }

    [HttpPost("GetById")]
    public async Task<IActionResult> GetByIdAsync([FromBody] TEntityPK key)
    {
        var result = await repository.GetByIdAsync(key);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(JsonHelper.SerializeWithMetadata(result));
    }

    private async Task<string> ReadBodyAsJsonAsync()
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body);
        Request.Body.Seek(0, SeekOrigin.Begin);
        return await reader.ReadToEndAsync();
    }

    [HttpPost("Add")]
    public async Task<IActionResult> AddAsync()
    {
        var json = await ReadBodyAsJsonAsync();
        var entity = JsonHelper.DeserializeWithMetadata<TEntity>(json);
        if (entity is null)
        {
            return BadRequest("Empty request");
        }

        var result = await repository.AddAsync(entity);
        return Created("", JsonHelper.SerializeWithMetadata(result));
    }

    [HttpPost("AddRange")]
    public async Task<IActionResult> AddRangeAsync()
    {
        var json = await ReadBodyAsJsonAsync();
        var entities = JsonHelper.DeserializeWithMetadata<TEntity[]>(json);
        if (entities is null)
        {
            return BadRequest("Empty request");
        }

        var result = await repository.AddAsync(entities);
        return Created("", JsonHelper.SerializeWithMetadata(result));
    }

    [HttpPost("Update")]
    public async Task<IActionResult> UpdateAsync()
    {
        var json = await ReadBodyAsJsonAsync();
        var model = JsonHelper.DeserializeWithMetadata<UpdateModel<TEntity>>(json);
        if (model is null)
        {
            return BadRequest("Empty request");
        }

        var result = await repository.UpdateAsync(model.Entity, model.OldEntity);
        return Accepted("", JsonHelper.SerializeWithMetadata(result));
    }

    [HttpPost("Count")]
    public async Task<IActionResult> CountAsync([FromBody] SerializedQueryData queryData)
    {
        var result = await repository.CountAsync(q => new SerializedQuery<TEntity>(queryData).Apply(q));
        return Ok(JsonHelper.SerializeWithMetadata(result));
    }

    [HttpPost("Sum")]
    public async Task<IActionResult> SumAsync([FromBody] SerializedQueryData queryData, SumType type)
    {
        var query = new SerializedQuery<TEntity>(queryData);
        object result = type switch
        {
            SumType.Int => await repository.SumAsync(q => query.Apply(q), query.SelectExpression<int>()),
            SumType.Double => await repository.SumAsync(q => query.Apply(q), query.SelectExpression<double>()),
            SumType.Float => await repository.SumAsync(q => query.Apply(q), query.SelectExpression<float>()),
            SumType.Decimal => await repository.SumAsync(q => query.Apply(q), query.SelectExpression<decimal>()),
            SumType.Long => await repository.SumAsync(q => query.Apply(q), query.SelectExpression<long>()),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown sum type")
        };

        return Ok(JsonHelper.SerializeWithMetadata(result));
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAsync(TEntityPK key)
    {
        try
        {
            await repository.DeleteAsync(key);
            return Accepted();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
}
