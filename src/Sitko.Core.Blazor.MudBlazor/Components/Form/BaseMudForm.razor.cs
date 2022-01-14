using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Sitko.Core.Blazor.Forms;

namespace Sitko.Core.Blazor.MudBlazorComponents;

public class MudEntityForm<TEntity> : BaseMudForm<TEntity>
    where TEntity : class, new()
{
    [EditorRequired] [Parameter] public RenderFragment<BaseMudForm<TEntity>> ChildContent { get; set; } = null!;

    protected override RenderFragment ChildContentFragment => ChildContent(this);
}

public abstract partial class BaseMudForm<TEntity>
    where TEntity : class, new()
{
    protected abstract RenderFragment ChildContentFragment { get; }
    protected MudEditForm<TEntity>? FormInstance { get; set; }
    [Inject] public ISnackbar Snackbar { get; set; } = null!;
    [Parameter] public bool Debug { get; set; }

    [EditorRequired] [Parameter] public Func<TEntity, Task<FormSaveResult>>? Add { get; set; }

    [EditorRequired] [Parameter] public Func<TEntity, Task<FormSaveResult>>? Update { get; set; }

    [EditorRequired] [Parameter] public Func<Task<(bool IsNew, TEntity Entity)>>? GetEntity { get; set; }

    protected override Task<(bool IsNew, TEntity Entity)> GetEntityAsync() => GetEntity!();

    protected override Task NotifySuccessAsync()
    {
        Snackbar.Add(LocalizationProvider["Entity saved successfully"], Severity.Success);
        return Task.CompletedTask;
    }

    protected override Task NotifyErrorAsync(string errorText)
    {
        Snackbar.Add(errorText, Severity.Error);
        return Task.CompletedTask;
    }

    protected override Task NotifyExceptionAsync(Exception ex)
    {
        Snackbar.Add(ex.ToString(), Severity.Error);
        return Task.CompletedTask;
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (GetEntity is null)
        {
            throw new InvalidOperationException("GetEntity is not defined");
        }

        if (Add is null)
        {
            throw new InvalidOperationException("Add is not defined");
        }

        if (Update is null)
        {
            throw new InvalidOperationException("Update is not defined");
        }
    }

    protected override Task<FormSaveResult> AddAsync(TEntity entity) => Add!(entity);

    protected override Task<FormSaveResult> UpdateAsync(TEntity entity) => Update!(entity);

    public override async Task ResetAsync()
    {
        await base.ResetAsync();
        FormInstance?.Reset();
    }
}
