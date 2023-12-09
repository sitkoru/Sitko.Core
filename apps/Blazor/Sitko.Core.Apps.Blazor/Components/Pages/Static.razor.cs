using Microsoft.AspNetCore.Components;
using Sitko.Core.Apps.Blazor.Data.Entities;
using Sitko.Core.Repository;

namespace Sitko.Core.Apps.Blazor.Components.Pages;

public partial class Static
{

    [Inject] private IRepository<BarModel, Guid> Repository { get; set; } = null!;
    private int barsCount;

    protected override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        barsCount = await Repository.CountAsync();
    }
}
