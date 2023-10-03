namespace Sitko.Core.Tasks;

public interface IBaseTaskWithConfigAndResult
{
    Type ConfigType { get; }
    Type ResultType { get; }
}