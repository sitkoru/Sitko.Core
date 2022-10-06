using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Sitko.Core.Repository;

namespace Sitko.Core.Blazor.MudBlazorComponents;

public class MudRepositoryForm<TEntity, TEntityPk> : BaseMudRepositoryForm<TEntity, TEntityPk,
    IRepository<TEntity, TEntityPk>>
    where TEntity : class, IEntity<TEntityPk>, new()
{
    [EditorRequired]
    [Parameter]
    public RenderFragment<MudRepositoryForm<TEntity, TEntityPk>> ChildContent { get; set; } = null!;

    protected override RenderFragment ChildContentFragment => ChildContent(this);
}

public abstract partial class BaseMudRepositoryForm<TEntity, TEntityPk, TRepository>
    where TEntity : class, IEntity<TEntityPk>, new()
    where TRepository : class, IRepository<TEntity, TEntityPk>
{
    protected MudEditForm<TEntity>? FormInstance { get; set; }
    [Parameter] public bool Debug { get; set; }
    [Inject] public ISnackbar Snackbar { get; set; } = null!;
    protected abstract RenderFragment ChildContentFragment { get; }

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
        FormInstance!.Reset();
        await base.ResetAsync();
    }
}
