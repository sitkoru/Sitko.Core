# Blazor components

[Русский](../ru/blazor.md) · [All topics](README.md)

## Server and browser integration

`Sitko.Core.Blazor` contains base components, forms, state and URL handling.
`Sitko.Core.Blazor.Server` connects the server through `AddSitkoCoreBlazorServer()`
and `MapSitkoCoreBlazor<TRootComponent>()`. `Sitko.Core.Blazor.Wasm` connects the
browser through `AddSitkoCoreBlazorWasm()` and runs the lifecycle with `RunApplicationAsync()`.
Mixed interactivity also needs `AddInteractiveWebAssembly()` and render-mode/
additional component assembly configuration, as shown in the [Blazor demo](blazor-demo.md).
The demo does not require an external database; form pages do not persist data
between openings.

| Packages prefixed with `Sitko.Core.` | Usage |
| --- | --- |
| `Blazor.MudBlazor`, `Blazor.MudBlazor.Server` | MudBlazor components: `AddMudBlazor()` on the Blazor builder; server integration through `AddMudBlazorServer()`. Component namespace: `Sitko.Core.Blazor.MudBlazorComponents`. |
| `Blazor.AntDesign`, `Blazor.AntDesign.Server` | AntDesign components; call `AddAntBlazorServer()` after Blazor Server registration. Component namespace: `Sitko.Core.Blazor.AntDesignComponents`. |
| `Blazor.FluentValidation` | `FluentValidator` inside `EditForm` receives the current `EditContext` and validates the model/fields through the registered `FluentGraphValidator`. |
| `Blazor.FileUpload` | `AddBlazorFileUpload()` and shared uploaders; the application supplies storage, server routes and limits. |

The application hosts the UI library's styles/scripts and required components.
Sitko.Core does not build custom AntDesign resources with webpack/npm; the old
`fileupload.js` reference is unnecessary. See [migration](migration-v13.md) and
[build configuration](build-configuration.md).
Server OIDC integration is not a browser token store; see
[OIDC/JWT](oidc-authentication.md) for authentication/state considerations.

## MudBlazor repository table

For a server-side repository, create a component derived from
`MudRepositoryTable<TEntity, TEntityPk, TRepository>`. `TEntity` implements
`IEntity<TEntityPk>`; `TRepository` implements `IRepository<TEntity,TEntityPk>`
and is available through DI. Supply content through `HeaderContent`, `ChildContent`
and `FooterContent`. Example declaration using your application types:

```csharp
public class BarRepositoryList :
    Sitko.Core.Blazor.MudBlazorComponents.MudRepositoryTable<BarModel, Guid, BarRepository>
{
}
```

Use `<BarRepositoryList>` with these fragments in Razor, rather than the standard
`MudTable`. `ConfigureQuery` is a `Func<IRepositoryQuery<TEntity>, Task>`:
add query conditions and return `Task.CompletedTask` when no asynchronous work is
needed. The table applies sorting and pagination itself; do not use your own
`Take`/`Skip` as a replacement for its pagination.

`EnableUrlNavigation=false` by default. With `true`, the table synchronizes search,
sorting, page and page size with the URL. Add custom filters with `AddParamsToUrl`
(`Func<Task<Dictionary<string,object?>>>`) and `GetParamsFromUrl` (`Func<Task>`).
Receive a page value through
`[Parameter, SupplyParameterFromQuery(Name = "newParamTitle")] public string? NewParam { get; set; }`.
The first callback returns `newParamTitle` with the current value; the second copies
`NewParam` into the filter and calls `StateHasChanged()` when it changes.
Check `NewParam`, not an unrelated title property.

For page size, declare `private int MyRowsPerPage { get; set; } = 10;` and bind
**that same property**: `<BarRepositoryList @bind-RowsPerPage="MyRowsPerPage">`.
Fragments are omitted; the application supplies entity/repository types.
The default page size is 50.

Verify using your repository's data: change the filter and page, refresh the URL
and use browser back navigation. Check an empty result set too. For forms/files,
repeat the [demo](blazor-demo.md) scenarios in Server and WebAssembly; a successful
build does not prove UI operation in both modes.

Sources: [table](../../src/Sitko.Core.Blazor.MudBlazor/Components/Table/MudTable.razor.cs),
[validation](../../src/Sitko.Core.Blazor.FluentValidation/FluentValidator.cs),
[server](../../src/Sitko.Core.Blazor.Server/ApplicationExtensions.cs),
[WebAssembly](../../src/Sitko.Core.Blazor.Wasm/WebAssemblyHostBuilderExtensions.cs).
