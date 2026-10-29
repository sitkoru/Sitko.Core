# Компоненты Blazor

[English](../en/blazor.md) · [Все темы](README.md)

## Выбор серверной и браузерной части

`Sitko.Core.Blazor` содержит базовые компоненты, формы, состояние и работу с URL.
`Sitko.Core.Blazor.Server` подключает сервер через `AddSitkoCoreBlazorServer()`
и `MapSitkoCoreBlazor<TRootComponent>()`. `Sitko.Core.Blazor.Wasm` подключает браузер
через `AddSitkoCoreBlazorWasm()` и запускает жизненный цикл через `RunApplicationAsync()`.
Для смешанной интерактивности также нужны `AddInteractiveWebAssembly()` и настройка
режимов/дополнительной сборки компонентов, как в [готовом Blazor-демо](blazor-demo.md).
Пример не требует внешней БД; страницы форм не сохраняют данные между открытиями.

| Пакеты с префиксом `Sitko.Core.` | Применение |
| --- | --- |
| `Blazor.MudBlazor`, `Blazor.MudBlazor.Server` | Компоненты MudBlazor: `AddMudBlazor()` на Blazor builder; серверная интеграция `AddMudBlazorServer()`. Namespace компонентов — `Sitko.Core.Blazor.MudBlazorComponents`. |
| `Blazor.AntDesign`, `Blazor.AntDesign.Server` | Компоненты AntDesign; сервер подключается через `AddAntBlazorServer()` после регистрации Blazor Server. Namespace компонентов — `Sitko.Core.Blazor.AntDesignComponents`. |
| `Blazor.FluentValidation` | `FluentValidator` внутри `EditForm` получает текущий `EditContext` и проверяет модель/поля через зарегистрированный `FluentGraphValidator`. |
| `Blazor.FileUpload` | `AddBlazorFileUpload()` и общие загрузчики; хранилище, серверные маршруты и лимиты задаёт приложение. |

Стили/скрипты UI-библиотеки и её необходимые компоненты размещаются в приложении.
Собственные ресурсы AntDesign через webpack/npm для Sitko.Core не собираются;
подключение старого `fileupload.js` не нужно. Действия перехода —
в [миграции](migration-v13.md), настройки сборки — [здесь](build-configuration.md).
Серверная OIDC-интеграция не является браузерным хранилищем токенов;
для входа/состояния см. [OIDC/JWT](oidc-authentication.md).

## Таблица репозитория MudBlazor

Для серверного репозитория создайте компонент, наследующийся от
`MudRepositoryTable<TEntity, TEntityPk, TRepository>`. Здесь `TEntity` реализует
`IEntity<TEntityPk>`, а `TRepository` — `IRepository<TEntity,TEntityPk>` и доступен
в DI. Содержимое задаётся через `HeaderContent`, `ChildContent` и `FooterContent`.
Пример объявления для собственных типов приложения:

```csharp
public class BarRepositoryList :
    Sitko.Core.Blazor.MudBlazorComponents.MudRepositoryTable<BarModel, Guid, BarRepository>
{
}
```

В Razor используйте `<BarRepositoryList>` с указанными фрагментами, а не обычный
`MudTable`. Callback `ConfigureQuery` имеет тип `Func<IRepositoryQuery<TEntity>, Task>`:
добавляйте условия к запросу, затем возвращайте `Task.CompletedTask`, если асинхронная
работа не требуется. Таблица сама применяет сортировку и страницы; не добавляйте
собственные `Take`/`Skip` как замену её пагинации.

`EnableUrlNavigation=false` по умолчанию. При `true` таблица синхронизирует поиск,
сортировку, страницу и размер страницы с URL. Для дополнительных фильтров задайте
`AddParamsToUrl` (`Func<Task<Dictionary<string,object?>>>`) и `GetParamsFromUrl`
(`Func<Task>`). На странице принимайте значение через
`[Parameter, SupplyParameterFromQuery(Name = "newParamTitle")] public string? NewParam { get; set; }`.
Первый callback возвращает `newParamTitle` с текущим значением; второй переносит
`NewParam` в фильтр и при изменении вызывает `StateHasChanged()`.
Проверяйте именно `NewParam`, а не несвязанное свойство заголовка.

Для размера страницы заведите `private int MyRowsPerPage { get; set; } = 10;`
и привяжите **тот же** параметр: `<BarRepositoryList @bind-RowsPerPage="MyRowsPerPage">`.
Содержимое фрагментов опущено; типы сущности/репозитория предоставляет приложение.
По умолчанию размер страницы равен 50.

Проверка: загрузите данные своего репозитория, измените фильтр и страницу,
обновите URL и используйте возврат браузера. Проверьте также пустой набор.
Для форм/файлов повторите сценарии [демо](blazor-demo.md) в Server и WebAssembly;
успешная сборка не подтверждает работу интерфейса в обоих режимах.

Источники: [таблица](../../src/Sitko.Core.Blazor.MudBlazor/Components/Table/MudTable.razor.cs),
[валидация](../../src/Sitko.Core.Blazor.FluentValidation/FluentValidator.cs),
[сервер](../../src/Sitko.Core.Blazor.Server/ApplicationExtensions.cs),
[WebAssembly](../../src/Sitko.Core.Blazor.Wasm/WebAssemblyHostBuilderExtensions.cs).
