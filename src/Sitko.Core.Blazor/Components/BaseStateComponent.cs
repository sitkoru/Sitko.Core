#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sitko.Core.App.Json;

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
#endif
