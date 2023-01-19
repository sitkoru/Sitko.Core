using Microsoft.AspNetCore.Components;

namespace Sitko.Core.Blazor.AntDesignComponents.Components;

public class AntStorageImageInput : BaseAntSingleStorageInput
{
    public override AntStorageInputMode Mode { get; set; } = AntStorageInputMode.Image;
    [Parameter] public override string ContentTypes { get; set; } = "image/jpeg,image/png,image/svg+xml";
}

