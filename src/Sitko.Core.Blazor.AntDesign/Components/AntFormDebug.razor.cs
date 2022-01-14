using Microsoft.AspNetCore.Components;
using Sitko.Core.App.Json;
using Sitko.Core.Blazor.Forms;

namespace Sitko.Core.Blazor.AntDesignComponents.Components;

public partial class AntFormDebug<TEntity> where TEntity : class, new()
{
#if NET6_0_OR_GREATER
    [EditorRequired]
#endif
    [Parameter]
    public BaseForm<TEntity> Form { get; set; } = null!;

    private string EntityJson => JsonHelper.SerializeWithMetadata(Form.Entity);
}
