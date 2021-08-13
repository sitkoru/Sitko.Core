using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using Sitko.Core.App.Blazor.Forms;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public partial class AntFormDebug<TEntity> where TEntity: class, new()
    {

#if NET6_0_OR_GREATER
        [EditorRequired]
#endif
        [Parameter]
        public BaseForm<TEntity> Form { get; set; } = null!;

        private string EntityJson
        {
            get
            {
                var settings = new JsonSerializerOptions { WriteIndented = true, MaxDepth = 10 };
#if NET6_0_OR_GREATER
                settings.ReferenceHandler = ReferenceHandler.IgnoreCycles;
#else
    settings.ReferenceHandler = ReferenceHandler.Preserve;
#endif
                return JsonSerializer.Serialize(Form.Entity, settings);
            }
        }
    }
}
