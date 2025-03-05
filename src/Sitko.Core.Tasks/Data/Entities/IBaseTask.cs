using System.ComponentModel.DataAnnotations.Schema;
using Sitko.Core.Queue.Kafka.Events;
using Sitko.Core.Repository;

namespace Sitko.Core.Tasks.Data.Entities;

public interface IBaseTask : IEntity<Guid>, IBaseEvent
{
    TaskStatus TaskStatus { get; set; }
    DateTimeOffset? ExecuteDateStart { get; set; }
    DateTimeOffset? ExecuteDateEnd { get; set; }
    string Type { get; set; }
    Guid? ParentId { get; set; }
    string? UserId { get; set; }
    DateTimeOffset? LastActivityDate { get; set; }
}

public interface IBaseTask<TConfig> : IBaseTask where TConfig : BaseTaskConfig, new()
{
    [Column(TypeName = "jsonb")] TConfig Config { get; set; }
}

public interface IBaseTask<TConfig, TResult> : IBaseTask<TConfig>, IBaseTaskWithConfigAndResult
    where TConfig : BaseTaskConfig, new() where TResult : BaseTaskResult
{
    [Column(TypeName = "jsonb")] TResult? Result { get; set; }

    Type IBaseTaskWithConfigAndResult.ConfigType => typeof(TConfig);
    Type IBaseTaskWithConfigAndResult.ResultType => typeof(TResult);
}
