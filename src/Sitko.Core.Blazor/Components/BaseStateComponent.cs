using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Sitko.Core.Blazor.Components;

public abstract class BaseStateComponent<TState> : BaseComponent where TState : BaseComponentState, new()
{
    [Inject] private ICompressedPersistentComponentState ComponentState { get; set; } = null!;
    protected TState State { get; set; } = new();
    private string StateKey => $"{GetType().Name}";

    protected override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        ComponentState.RegisterOnPersisting(async () => { await ComponentState.PersistAsBytesAsync(StateKey, State); });

        var componentState = await ComponentState.TryTakeFromBytesAsync<TState>(StateKey);
        if (componentState.isSuccess)
        {
            State = componentState.data!;
        }
        else
        {
            State = await LoadStateAsync();
        }
    }

    protected abstract Task<TState> LoadStateAsync();
}

public abstract class BaseComponentState
{
}
