using System.Reflection;
using Sitko.Core.Tasks.Data.Entities;
using TaskStatus = Sitko.Core.Tasks.Data.Entities.TaskStatus;

namespace Sitko.Core.Tasks;

internal static class TasksExtensions
{
    private static readonly MethodInfo? SetErrorResultMethodInfo =
        typeof(TasksExtensions).GetMethod(nameof(SetErrorResult), BindingFlags.Static | BindingFlags.Public);

    public static void SetErrorResult<TTask, TConfig, TResult>(TTask task, string error)
        where TTask : IBaseTask<TConfig, TResult>
        where TConfig : BaseTaskConfig, new()
        where TResult : BaseTaskResult, new()
    {
        var result = new TResult { ErrorMessage = error, IsSuccess = false };
        task.TaskStatus = TaskStatus.Fails;
        task.Result = result;
        task.ExecuteDateEnd = DateTimeOffset.UtcNow;
    }

    public static void SetTaskErrorResult(IBaseTaskWithConfigAndResult task, string error)
    {
        var scheduleMethod = SetErrorResultMethodInfo!.MakeGenericMethod(task.GetType(),
            task.ConfigType, task.ResultType);
        scheduleMethod.Invoke(null, new object?[] { task, error });
    }
}

