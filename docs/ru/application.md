# Приложение и модули

[English](../en/application.md) · [Все темы](README.md)

`Sitko.Core.App` подключает модули к стандартному .NET Host. Каждый модуль
регистрирует зависимости, читает свою секцию конфигурации и участвует в жизненном
цикле приложения. `Sitko.Core.App.Web` добавляет интеграцию ASP.NET Core.
Нужны .NET 10 и ссылки на используемые проекты/пакеты; версия среды репозитория —
в [руководстве сборки](build-configuration.md).

## Минимальный сервер

В проекте с `Microsoft.NET.Sdk.Web` и ссылкой на `Sitko.Core.App.Web`:

```csharp
using Sitko.Core.App.Web;

var builder = WebApplication.CreateBuilder(args);
builder.AddSitkoCoreWeb();
var app = builder.Build();
app.MapSitkoCore();
app.MapGet("/hello", () => "Hello from Sitko.Core");
app.Run();
```

После запуска запрос `/hello` возвращает текст примера. Для процесса без HTTP
создайте `Host.CreateApplicationBuilder(args)`, вызовите `AddSitkoCore()` из
`Sitko.Core.App`, соберите Host и выполните `RunAsync()`. Одного `Build()` недостаточно
для инициализации модулей.

## Конфигурация и свой модуль

Секция `Application` содержит `Name`, `Version`, `Environment` и
`EnableConsoleLogging`. Без явных значений имя/версия берутся из входной сборки,
окружение — из Host; консольное логирование включено в Development.
Серверный builder также читает необязательный `appsettings.local.json` без
автоматической перезагрузки этого файла. Не сохраняйте в нём секреты в Git.

Для собственного модуля наследуйте `BaseApplicationModule<TOptions>`, где
`TOptions : BaseModuleOptions, new()`, задайте `OptionsKey` и подключите
`AddModule<TModule, TOptions>()`. Зависимости регистрируются в `ConfigureServices`,
проверки/подготовка окружения — в `InitAsync`. `Enabled=false` выключает модуль.
По умолчанию повторная регистрация того же типа запрещена. Требуемые модули из
`GetRequiredModules` проверяются при запуске; их отсутствие останавливает запуск.

Модуль читает секции из `OptionKeys` по порядку, затем выполняет `Configure` его
options и callback регистрации. Для скалярных настроек позднее значение заменяет
раннее; коллекции привязывает стандартный .NET binder, общей гарантии замены
списка нет. `optionsKey` выбирает другую секцию. Настройки инициализации следует
проверять после перезапуска; наличие `IOptionsMonitor` само по себе не означает,
что модуль перестраивает соединения и подписки при изменении настроек.

## HTTP и локализация

`MapSitkoCore()` подключает middleware модулей и HTTP-проверки состояния:
`/health/all`, `/health/startup`, `/healthz`, `/ready`. Состав проверок зависит от
тегов: проверки БД/S3 по умолчанию исключены из liveness/readiness. Поэтому
`/ready` не доказывает доступность всех внешних сервисов.
Аутентификация подключается для зарегистрированных auth-модулей; правила
доступа задаёт приложение. См. [OIDC/JWT](oidc-authentication.md).

`AddJsonLocalization()` из `Sitko.Core.App.Localization` подключает JSON-ресурсы
для `IStringLocalizer`; `AddDefaultResource<T>()` задаёт общие ресурсы.
Параметр `Localization:Json:CacheTimeInMinutes` по умолчанию равен 60.
При отсутствии перевода возвращается сам ключ. Формат и включение ресурсов
показаны в [проверках локализации](../../tests/Sitko.Core.App.Tests/ILocalizationTests.cs).
Выбор культуры HTTP настраивается через `ConfigureLocalization(...)` из App.Web.

Источники: [builder](../../src/Sitko.Core.App/SitkoCoreBaseApplicationBuilder.cs),
[жизненный цикл](../../src/Sitko.Core.App/ApplicationLifecycle.cs),
[параметры](../../src/Sitko.Core.App/Helpers/OptionsHelper.cs),
[HTTP-интеграция](../../src/Sitko.Core.App.Web/WebApplicationBuilderExtensions.cs).
