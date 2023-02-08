using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sitko.Core.App.Json;

namespace Sitko.Core.Repository.Remote.Server;

public class BaseRemoteRepositoryController<TEntity, TEntityPK> : Controller
    where TEntity : class, IEntity<TEntityPK> where TEntityPK : notnull
{
    private readonly IRepository<TEntity, TEntityPK> repository;

    public BaseRemoteRepositoryController(IRepository<TEntity, TEntityPK> repository,
        ILogger<BaseRemoteRepositoryController<TEntity, TEntityPK>> logger)
    {
        Logger = logger;
        this.repository = repository;
    }

    protected ILogger<BaseRemoteRepositoryController<TEntity, TEntityPK>> Logger { get; }


    [HttpPost("GetAll")]
    public async Task<IActionResult> GetAllAsync([FromBody] SerializedQueryDataRequest queryData)
    {
        try
        {
            var result = await repository.GetAllAsync(repositoryQuery =>
                new SerializedQuery<TEntity>(queryData.Data).Apply(repositoryQuery));
            return Ok(JsonHelper.SerializeWithMetadata(new ListResult<TEntity>(result.items, result.itemsCount)));
        }
        catch (Exception ex)
        {
            return Error(ex);
        }
    }

    [HttpPost("Get")]
    public async Task<IActionResult> GetAsync([FromBody] SerializedQueryDataRequest queryDataJson)
    {
        try
        {
            var queryData = queryDataJson.Data;
            var result = await repository.GetAsync(repositoryQuery =>
                new SerializedQuery<TEntity>(queryData).Apply(repositoryQuery));
            if (result is null)
            {
                return NotFound();
            }

            return Ok(JsonHelper.SerializeWithMetadata(result));
        }
        catch (Exception ex)
        {
            return Error(ex);
        }
    }

    [HttpPost("GetById")]
    public async Task<IActionResult> GetByIdAsync([FromBody] TEntityPK key)
    {
        try
        {
            var result = await repository.GetByIdAsync(key);
            if (result is null)
            {
                return NotFound();
            }

            return Ok(JsonHelper.SerializeWithMetadata(result));
        }
        catch (Exception ex)
        {
            return Error(ex);
        }
    }

    private async Task<string> ReadBodyAsJsonAsync()
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body);
        Request.Body.Seek(0, SeekOrigin.Begin);
        return await reader.ReadToEndAsync();
    }

    private IActionResult Error(Exception ex, [CallerMemberName] string methodName = "")
    {
        Logger.LogError(ex, "Error in method {MethodName}: {ErrorText}", methodName, ex.ToString());
        return BadRequest(ex.ToString());
    }

    private IActionResult Error(string error, [CallerMemberName] string methodName = "")
    {
        Logger.LogError("Error in method {MethodName}: {ErrorText}", methodName, error);
        return BadRequest(error);
    }

    [HttpPost("Add")]
    public async Task<IActionResult> AddAsync()
    {
        try
        {
            var json = await ReadBodyAsJsonAsync();
            var entity = JsonHelper.DeserializeWithMetadata<TEntity>(json);
            if (entity is null)
            {
                return Error("Empty request");
            }

            var result = await repository.AddAsync(entity);
            return Created("", JsonHelper.SerializeWithMetadata(result));
        }
        catch (Exception ex)
        {
            return Error(ex);
        }
    }

    [HttpPost("AddRange")]
    public async Task<IActionResult> AddRangeAsync()
    {
        try
        {
            var json = await ReadBodyAsJsonAsync();
            var entities = JsonHelper.DeserializeWithMetadata<TEntity[]>(json);
            if (entities is null)
            {
                return Error("Empty request");
            }

            var result = await repository.AddAsync(entities);
            return Created("", JsonHelper.SerializeWithMetadata(result));
        }
        catch (Exception ex)
        {
            return Error(ex);
        }
    }

    [HttpPost("Update")]
    public async Task<IActionResult> UpdateAsync()
    {
        try
        {
            var json = await ReadBodyAsJsonAsync();
            var model = JsonHelper.DeserializeWithMetadata<UpdateModel<TEntity>>(json);
            if (model is null)
            {
                return Error("Empty request");
            }

            var result = await repository.UpdateAsync(model.Entity, model.OldEntity);
            return Accepted("", JsonHelper.SerializeWithMetadata(result));
        }
        catch (Exception ex)
        {
            return Error(ex);
        }
    }

    [HttpPost("Count")]
    public async Task<IActionResult> CountAsync([FromBody] SerializedQueryDataRequest queryData)
    {
        try
        {
            var result = await repository.CountAsync(q => new SerializedQuery<TEntity>(queryData.Data).Apply(q));
            return Ok(JsonHelper.SerializeWithMetadata(result));
        }
        catch (Exception ex)
        {
            return Error(ex);
        }
    }

    [HttpPost("Sum")]
    public async Task<IActionResult> SumAsync([FromBody] SerializedQueryDataRequest queryData, SumType type)
    {
        try
        {
            var query = new SerializedQuery<TEntity>(queryData.Data);
            object result = type switch
            {
                SumType.TypeInt => await repository.SumAsync(q => query.Apply(q), query.SelectExpression<int>()),
                SumType.TypeDouble => await repository.SumAsync(q => query.Apply(q), query.SelectExpression<double>()),
                SumType.TypeFloat => await repository.SumAsync(q => query.Apply(q), query.SelectExpression<float>()),
                SumType.TypeDecimal =>
                    await repository.SumAsync(q => query.Apply(q), query.SelectExpression<decimal>()),
                SumType.TypeLong => await repository.SumAsync(q => query.Apply(q), query.SelectExpression<long>()),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown sum type")
            };

            return Ok(JsonHelper.SerializeWithMetadata(result));
        }
        catch (Exception ex)
        {
            return Error(ex);
        }
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAsync(TEntityPK key)
    {
        try
        {
            await repository.DeleteAsync(key);
            return Accepted();
        }
        catch (Exception ex)
        {
            return Error(ex);
        }
    }
}
