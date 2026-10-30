# Локальные учётные записи ASP.NET Core Identity

[English](../en/identity.md) · [Все темы](README.md)

`Sitko.Core.Identity` подключает ASP.NET Core Identity с EF-хранилищем пользователей
и ролей. Это самостоятельная возможность для локальных учётных записей.
Для внешнего поставщика входа используйте [OIDC/JWT](oidc-authentication.md).
`Sitko.Core.Auth` содержит общие параметры cookie, политик и Data Protection,
используемые модулями Identity и OIDC; это не отдельный готовый поставщик входа.

После [регистрации веб-приложения](application.md) вызовите
`AddIdentity<TUser, TRole, TPk, TDbContext>(...)` из `Sitko.Core.Identity`.
Нужны свои `IdentityUser<TPk>`, `IdentityRole<TPk>` и
`IdentityDbContext<TUser,TRole,TPk>`. Контекст БД, миграции и соединение регистрируются
приложением отдельно, например через [PostgreSQL-модуль](data.md).

Секция `Identity`:

| Параметр | По умолчанию / назначение |
| --- | --- |
| `RequireConfirmedAccount` | `true`: вход требует подтверждённой учётной записи. |
| `AddDefaultUi` | `false`; при `true` регистрирует стандартный Identity UI и Razor Pages. |
| `LoginPath` | Поле options со значением `/Identity/Account/Login`; ограничение применения ниже. |
| `LogoutPath` | Поле options со значением `/Identity/Account/Logout`; ограничение применения ниже. |
| `Policies`, `ForcePolicy` | Именованные политики и выбранная резервная политика. |
| `EnableRedisDataProtection` | Хранение ключей в Redis; соединение и условия сохранности — в [инструкции Redis](redis-sentinel.md). |

Поля `Identity:LoginPath` и `Identity:LogoutPath` сейчас не переопределяют cookie
Identity: модуль возвращает `RequiresAuthentication=false`, поэтому общая ветвь
`AuthModule.ConfigureCookieOptions` не вызывается. При необходимости настраивайте
пути через стандартные параметры cookie ASP.NET Core Identity в приложении.

После Build вызовите `MapSitkoCore()`: для `AddDefaultUi=true` Identity-модуль
подключает Razor Pages. Если UI выключен, собственные страницы и действия входа,
выхода, подтверждения и восстановления пароля реализует приложение. Регистрация
модуля не создаёт пользователя и не подтверждает его адрес.

Проверьте создание пользователя в отдельной БД, запрет неподтверждённой записи,
успешный вход после подтверждения, выход и ограничение доступа политикой.
Проверки роли и прикладных разрешений не заменяются наличием cookie.
Доставка писем подтверждения и настройки доступа входят в ответственность приложения.

Источники: [регистрация](../../src/Sitko.Core.Identity/ApplicationExtensions.cs),
[модуль и параметры](../../src/Sitko.Core.Identity/IdentityModule.cs),
[общая Auth-основа](../../src/Sitko.Core.Auth/AuthModule.cs).
