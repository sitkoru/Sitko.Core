using Microsoft.AspNetCore.Components;
using MudBlazor;
using Sitko.Core.Blazor.Forms;
using Sitko.Core.Repository;

// ReSharper disable once CheckNamespace
namespace Sitko.Core.Blazor.MudBlazorComponents;

public abstract partial class BaseMudRepositoryForm<TEntity, TEntityPk, TRepository>
    where TEntity : class, IEntity<TEntityPk>, new()
    where TRepository : class, IRepository<TEntity, TEntityPk>
    where TEntityPk : notnull
{
    protected MudEditForm<TEntity>? FormInstance { get; set; }
    [Parameter] public bool Debug { get; set; }
    [Inject] public ISnackbar Snackbar { get; set; } = null!;

    [Parameter] [EditorRequired] public RenderFragment<FormContext<TEntity>> ChildContent { get; set; } = null!;

    [Parameter] public RenderFragment? LoadingContent { get; set; }

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

    public override async Task ResetAsync()
    {
        await StartLoadingAsync();
        await base.ResetAsync();
        await StopLoadingAsync();
    }
}
