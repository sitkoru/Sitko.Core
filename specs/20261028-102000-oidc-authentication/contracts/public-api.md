# Публичный контракт

Пакет и пространство имён: Sitko.Core.Auth.OIDC. Регистрации AddOidcAuth/AddJwtAuth
для IHostApplicationBuilder и ISitkoCoreWebApplicationBuilder сохраняют варианты callback
и optionsKey. OidcAuthModule/OidcAuthModuleOptions, JwtAuthModule/JwtAuthModuleOptions.
Общая база: OidcProviderModule<TOptions>/OidcProviderAuthOptions. Validators получают
соответствующее имя без IdentityServer. IUserTokenProvider и TokenStoreType переносятся
в пространство Tokens без изменения сигнатуры получения токена и чисел enum.
Настройки по умолчанию Auth:OIDC:Client и Auth:OIDC:Jwt. Схемы Cookies/oidc/Bearer.
Свойство ConfigureOpenIdConnectOptions позволяет настроить штатные options.
Старые имена автоматически не поддерживаются. Полный переход описывается в docs RU/EN.
