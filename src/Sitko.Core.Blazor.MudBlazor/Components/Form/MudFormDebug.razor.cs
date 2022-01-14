using Microsoft.AspNetCore.Components;
using Sitko.Core.App.Json;
using Sitko.Core.Blazor.Forms;

namespace Sitko.Core.Blazor.MudBlazorComponents;

public partial class MudFormDebug<TEntity> where TEntity : class, new()
{
    [EditorRequired] [Parameter] public BaseForm<TEntity> Form { get; set; } = null!;

    private string EntityJson => JsonHelper.SerializeWithMetadata(Form.Entity);
}
