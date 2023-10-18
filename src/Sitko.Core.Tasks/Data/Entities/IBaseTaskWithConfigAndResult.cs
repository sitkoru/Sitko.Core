namespace Sitko.Core.Tasks.Data.Entities;

public interface IBaseTaskWithConfigAndResult
{
    Type ConfigType { get; }
    Type ResultType { get; }
}