# Аутентификация OIDC и JWT

[English](../en/oidc-authentication.md) · [Переход на версию 13](migration-v13.md)

`Sitko.Core.Auth.OIDC` подключает стандартные обработчики ASP.NET Core для входа
через OpenID Connect и проверки входящих JWT. Модуль рассчитан на серверное
приложение .NET 10. Поставщик может быть Keycloak или другим совместимым сервером.
Допуск пользователей и права API настраиваются отдельно от проверки подлинности.

## Подключение веб-приложения

Пример использует клиент `admin-web` в realm `example`; адреса и имена замените
значениями своего приложения. Секрет передайте через секретное хранилище или
`Auth__OIDC__Client__OidcClientSecret`, не сохраняйте его в репозитории.

```csharp
using Sitko.Core.App.Web;
using Sitko.Core.Auth.OIDC;

var builder = WebApplication.CreateBuilder(args);
builder.AddSitkoCoreWeb().AddOidcAuth();
var app = builder.Build();
app.MapSitkoCore();
// Здесь приложение регистрирует защищённые страницы и обработчики входа/выхода.
app.Run();
```

```json
{
  "Auth": {
    "OIDC": {
      "Client": {
        "OidcServerUrl": "https://sso.example.com/realms/example",
        "OidcClientId": "admin-web",
        "RequireHttps": true,
        "OidcScopes": ["email"],
        "TokenStoreType": "None"
      }
    }
  }
}
```

`OidcServerUrl` допускает адрес issuer/realm с завершающим `/` и без него.
При включённом healthcheck discovery-документ запрашивается внутри этого пути:
`https://sso.example.com/realms/example/.well-known/openid-configuration`.
Нормализация адреса healthcheck не изменяет `OidcServerUrl` и `Authority`.

По умолчанию используются схемы `Cookies` и `oidc`, Code Flow с PKCE,
`SaveTokens = true`, `GetClaimsFromUserInfoEndpoint = true`; к стандартным scopes
добавляется `offline_access`. Разрешите нужные scopes у поставщика; если автономное
обновление не требуется, удалите `offline_access` через настройку ниже.
`RequireHttps` по умолчанию `false` ради существующей конфигурации; для рабочего
окружения задайте `true`. Cookie всегда имеет `Secure`, поэтому используйте HTTPS
и при локальной проверке браузера. Прямое HTTP-соединение к Keycloak в локальном
стенде не снимает это требование для адреса приложения.

Стандартные параметры доступны через callback:

```csharp
builder.AddOidcAuth(options =>
{
    options.ConfigureOpenIdConnectOptions = oidc =>
    {
        oidc.CallbackPath = "/signin-oidc";
        oidc.Scope.Remove("offline_access");
        // Пример явного выбора исходных имён claims:
        oidc.MapInboundClaims = false;
        oidc.TokenValidationParameters.NameClaimType = "name";
    };
    options.UserIdClaimName = "sub";
});
```

Это альтернативная форма регистрации после `AddSitkoCoreWeb()`, а не второй вызов
в дополнение к первому примеру. Если меняете отображение claims, согласуйте
`UserIdClaimName` с фактическим principal. По умолчанию это `ClaimTypes.NameIdentifier`.
В обеих формах доступны callback с `IApplicationContext` и аргумент `optionsKey`
для другой секции конфигурации. Повторное подключение нескольких OIDC-провайдеров
под фиксированной схемой `oidc` не поддерживается этим API.

Для пользовательских событий задавайте `oidc.Events`. При управляемом хранилище
`EventsType` отклоняется при создании параметров, чтобы не выключить внутреннюю
обработку. Отказ в `OnTokenValidated` задаётся через `context.Fail(...)`.
В `OnTicketReceived` завершайте ответ через `context.HandleResponse()` либо
передавайте обработку дальше через `SkipHandler()` согласно контракту ASP.NET Core;
один `Fail()` в этом позднем событии не заменяет завершение ответа. Токены сохраняются
после успешного протокола, UserInfo и пользовательского `OnTicketReceived`.

## JWT и политики

Для API подключите `AddJwtAuth()` с секцией `Auth:OIDC:Jwt`:

```json
{
  "Auth": {
    "OIDC": {
      "Jwt": {
        "OidcServerUrl": "https://sso.example.com/realms/example",
        "JwtAudience": "example-api",
        "RequireHttps": true
      }
    }
  }
}
```

Поставщик должен включать `example-api` в `aud` выдаваемого API-токена. Схема
`Bearer` проверяет подпись, issuer, audience и срок; `ValidIssuers` и
`ConfigureJwtBearerOptions` сохраняют возможность прикладной настройки.
JWT сохраняет исходные имена claims (`MapInboundClaims = false`); callback может
явно изменить это поведение.

При совместном OIDC/JWT выбирайте схемы явно в политиках, не полагайтесь на порядок
модулей и схему по умолчанию:

```csharp
builder.AddOidcAuth();
builder.AddJwtAuth(options => options.Policies["api"] =
    new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder("Bearer")
        .RequireAuthenticatedUser().Build());
// После Build(): app.MapGet("/api/example", ...).RequireAuthorization("api");
```

Аналогично политика страниц выбирает `Cookies`. Роль клиента Keycloak, разрешающая
вход, не заменяет прикладные политики API. `resource_access` — вложенный JSON;
он не становится автоматически набором плоских role claims. Если приложению нужен
`RequireRole`, настройте protocol mapper и `RoleClaimType` согласованно на обеих
сторонах и проверьте настоящий токен. Не отключайте проверку issuer/audience
ради прохождения входа.

## Токены и выход

`IUserTokenProvider.GetUserTokenAsync(HttpContext, CancellationToken)` возвращает
access token текущего запроса либо `null`, если получить его нельзя.

| `TokenStoreType` | Поведение |
| --- | --- |
| `None` (`0`, по умолчанию) | Читает токен, сохранённый обработчиком аутентификации. Автоматического refresh нет; при `SaveTokens = false` сохранённого токена не будет. |
| `Redis` (`1`) | Duende обновляет токены и хранит их в Redis. Настройка соединения и Data Protection — в [инструкции Redis](redis-sentinel.md). |
| `InMemory` (`2`) | Duende обновляет токены; содержимое теряется при остановке процесса и не разделяется между репликами. |

При управляемом хранилище отказ refresh не подменяется прежним токеном cookie.
Вызывающий код должен обработать `null` и при необходимости инициировать новый вход.
`ConfigureUserTokenManagement` получает параметры Duende 4.

Вход и выход — действия приложения. Для входа выполните `ChallengeAsync("oidc")`.
Для локального выхода — `SignOutAsync("Cookies")`; с управлением токенами это также
запрашивает отзыв refresh token и очищает хранилище. Для завершения сессии поставщика
дополнительно выполните `SignOutAsync("oidc", properties)`. Запросы выхода оформляйте
защищённым от CSRF действием приложения; `RedirectUri` должен быть локальным доверенным
адресом. Модуль не добавляет публичные маршруты входа/выхода автоматически.

Срок cookie по умолчанию — 30 дней со скользящим продлением (`CookieExpireInMinutes`).
Отзыв роли или сессии в Keycloak не удаляет уже выданную cookie приложения. Мгновенный
запрет требует отдельной прикладной проверки/отзыва сессий; эта интеграция его не обещает.

Redis сохраняет прежний ключ: SHA-256 от `DOT_{Application.Name}_{Environment}_{userId}`,
purpose `RedisTokenStore` и строковые поля JSON. Для чтения старых записей сохраните
имя приложения, окружение, claim пользователя, Redis DB и ключи Data Protection.
Хранилище остаётся общим для одного пользователя приложения, а не отдельным для каждой
браузерной сессии: выход одной сессии может очистить токены других сессий того же пользователя.

## Допуск клиента в Keycloak

Одна роль сама по себе не запрещает вход. Нужен исполняемый запрет в потоках
аутентификации Keycloak. Следующий пример — инструкция для отдельной миграции
потребителя; живая конфигурация Keycloak этой поставкой не проверялась.

Пример: клиент `admin-web`, роль клиента `access`, технический client scope
`admin-web-access-check`. Scope лишь выбирает клиент для проверки; право даёт роль.

1. Создайте серверный OIDC-клиент с `Client authentication` и `Standard flow`.
   Задайте точные redirect URI (`https://admin.example.com/signin-oidc`) и post-logout
   URI (`https://admin.example.com/signout-callback-oidc`), PKCE `S256`. Отключите
   ненужные `Direct access grants`, `Implicit flow` и `Service accounts`.
2. Создайте роль `access` у `admin-web`. Выдайте её выбранным пользователям или
   группе. Не делайте её ролью по умолчанию для всех пользователей realm.
3. Создайте `admin-web-access-check` и назначьте его **default client scope только
   этому клиенту**. Для обязательной проверки он не должен быть optional.
4. Создайте копию Browser flow и назначьте её этому клиенту через
   `Authentication flow overrides` → `Browser Flow`. Сначала REQUIRED-подпоток
   аутентифицирует пользователя: внутри находятся альтернативы Cookie, формы и
   перенаправление к внешнему IdP. Затем отдельный CONDITIONAL-подпоток запрещает
   доступ при отсутствии роли. Cookie не должна быть альтернативой этому второму
   подпотоку на верхнем уровне, иначе SSO сможет закончить вход до проверки.
5. В подпотоке запрета задайте REQUIRED-условия `Condition - client scope`
   (`admin-web-access-check`, без отрицания) и `Condition - User Role`
   (`admin-web.access`, `Negate output` включён), затем REQUIRED `Deny Access`.
6. Для каждого используемого внешнего IdP добавьте такую же проверку в его
   `Post login flow`, сохранив существующие шаги. Browser flow после успешного входа
   через IdP не продолжается; одной проверки внутри Browser недостаточно.
   Условие default client scope ограничивает этот общий post-login поток нужным
   клиентом и не блокирует остальные приложения.
7. Проверьте остальные включённые пути входа (регистрация, reset credentials,
   device/CIBA, token exchange). Они не получают защиту автоматически от Browser
   override. Не открывайте неподтверждённые пути для этого клиента: отключите их
   либо настройте и отдельно проверьте тот же запрет.

Основания: [условия и запрет доступа](https://www.keycloak.org/docs/latest/server_admin/index.html#_authentication-flows),
[внешний IdP и Post login flow](https://www.keycloak.org/docs/26.7.5/server_admin/#_identity_broker),
[условие client scope](https://www.keycloak.org/docs/26.5.7/server_admin/index.html#_authentication-flows).
Имена экранов зависят от установленной версии; перед применением проверьте её
документацию. Настройка `Authorization` клиента и добавление роли в токен сами по
себе не заменяют запрет входа.

Обязательная приёмка до удаления старого ограничения:

| Сценарий | Результат |
| --- | --- |
| Новый вход пользователя с ролью | Cookie приложения выдана; нужные страницы доступны. |
| Новый вход без роли | Keycloak отказывает; cookie приложения не выдаётся. |
| Уже есть SSO от другого клиента, роли нет | Вход в `admin-web` запрещён. |
| Google/другой IdP, включая повторный вход | Допуск зависит от роли клиента, а не от наличия внешней учётной записи. |
| Роль снята, cookie приложения удалена, SSO сохранена | Повторный вход запрещён. |
| Другой клиент realm | Его прежний допуск не изменился. |
| Роль снята при живой cookie приложения | Зафиксировано принятое время сохранения доступа; мгновенный отзыв не заявляется. |

## Переход потребителя

| Версия 12 | Версия 13 |
| --- | --- |
| Пакет/namespace `Sitko.Core.Auth.IdentityServer` | `Sitko.Core.Auth.OIDC` |
| `AddOidcIdentityServer` / `AddJwtIdentityServer` | `AddOidcAuth` / `AddJwtAuth` |
| `OidcIdentityServerModule` / `OidcIdentityServerModuleOptions` | `OidcAuthModule` / `OidcAuthModuleOptions` |
| `JwtIdentityServerModule` / `JwtIdentityServerModuleOptions` | `JwtAuthModule` / `JwtAuthModuleOptions` |
| `IdentityServerModule<T>` / `IdentityServerAuthOptions` | `OidcProviderModule<T>` / `OidcProviderAuthOptions` |
| `IdentityServerAuthOptionsValidator<T>` | `OidcProviderAuthOptionsValidator<T>` |
| `Auth:IdentityServer:Oidc` / `Auth:IdentityServer:Jwt` | `Auth:OIDC:Client` / `Auth:OIDC:Jwt` |
| Namespace `.Auth.IdentityServer.Tokens` | `.Auth.OIDC.Tokens` |

`OidcAuthOptionsValidator`, `JwtAuthOptionsValidator`, `IUserTokenProvider`,
`OidcEvents` и `TokenStoreType` сохраняют короткие имена в новом namespace.
Автоматических aliases для пакетов, типов и секций нет. Значение собственного
`optionsKey` сохраняйте явно. Если приложение использует API Duende напрямую,
мигрируйте его по [руководству Duende 4](https://docs.duendesoftware.com/accesstokenmanagement/upgrading/atm-v3-to-v4/):
менеджер — `IUserTokenManager`, токены типизированы, результаты операций — `TokenResult`.
Собственные реализации `IUserTokenStore` требуют адаптации.

При переходе с Google сначала настройте роль и все потоки допуска в Keycloak,
затем замените пакет/регистрацию/конфигурацию и проверьте таблицу приёмки выше.
Непустой прежний `Users` был ограничением по точному email; его удаление без нового
запрета расширяет доступ. Назначайте роли проверенным учётным записям, не копируйте
email-строки в произвольный claim без проверки связывания пользователя. Значение `sub`
после смены поставщика может измениться: отдельно перенесите прикладные связи с
пользователем. Старая Google cookie не доказывает допуск Keycloak; при переключении
завершите прежние сессии или смените имя cookie и проведите новый вход.

Для Blazor Server интерактивный circuit не равен HTTP-запросу. Получайте токен через
`IUserTokenProvider` там, где есть актуальный `HttpContext`; модуль не добавляет
обновление authentication state/circuit или браузерное хранилище токенов. Проверьте
полную перезагрузку после входа/выхода, reconnect и истечение сессии в конкретном
приложении. Blazor WebAssembly требует отдельной клиентской OIDC-интеграции;
серверный client secret туда не передаётся.

Локальные автоматические проверки подтверждают обработчики ASP.NET Core, PKCE,
callback/UserInfo, JWT, refresh/revoke и совместимость хранилищ в пределах своих
стендов. Они не подтверждают действующие настройки Keycloak или готовность конкретного
потребителя. `Sitko.Core.Identity` сохраняет отдельный контракт и не мигрирует этой задачей.
