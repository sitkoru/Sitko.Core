using Sitko.Core.Tasks.Data.Entities;

namespace MudBlazorUnited.Tasks;

public abstract record MudBlazorBaseTask : BaseTask;

public abstract record MudBlazorBaseTask<TConfig, TResult> : MudBlazorBaseTask, IBaseTask<TConfig, TResult>
    where TConfig : BaseTaskConfig, new() where TResult : BaseTaskResult
{
    public TConfig Config { get; set; } = default!;
    public TResult? Result { get; set; }
}
