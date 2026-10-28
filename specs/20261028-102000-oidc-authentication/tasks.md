# Задачи: OIDC/JWT

## Подготовка
- [x] T001 Проверить основу, исходные тесты и контракты в specs/20261028-102000-oidc-authentication/research.md.
- [x] T002 Зафиксировать требования и совместимость в specs/20261028-102000-oidc-authentication/spec.md и data-model.md.

## Общая основа
- [x] T003 Переименовать публичный API и проект src/Sitko.Core.Auth.IdentityServer в src/Sitko.Core.Auth.OIDC, обновить Sitko.Core.sln и ссылки tests/Sitko.Core.Auth.Tests.
- [x] T004 Обновить Duende и прямой JwtBearer в Directory.Packages.props и src/Sitko.Core.Auth.OIDC/Sitko.Core.Auth.OIDC.csproj.

## US1: вход в веб-приложение
Независимая проверка: настоящий OIDC handler с изолированным поставщиком, cookie и PKCE.
- [x] T005 [US1] Добавить по одному сценарию challenge, callback, отказа и logout в tests/Sitko.Core.Auth.Tests/OidcAuthenticationTests.cs, проверить до изменения поведения.
- [x] T006 [US1] Добавить настройку OIDC и безопасный порядок сохранения токенов в src/Sitko.Core.Auth.OIDC/OidcAuthModule.cs и Tokens/OidcEvents.cs.

## US2: API и токены
Независимая проверка: JWT handler и зарегистрированные IUserTokenProvider/IUserTokenStore.
- [x] T007 [US2] Проверить получение/refresh/revoke и ошибки через tests/Sitko.Core.Auth.Tests/OidcAuthenticationTests.cs; адаптировать Tokens/UserTokenProvider.cs.
- [x] T008 [US2] Проверить прежние строковые JSON-поля, отсутствие идентификатора, store/get/clear в tests/Sitko.Core.Auth.Tests/TokenStoreTests.cs; адаптировать Tokens/InMemoryTokenStore.cs и Tokens/RedisTokenStore.cs.
- [x] T009 [US2] Проверить подпись/issuer/audience/lifetime и совместную регистрацию в tests/Sitko.Core.Auth.Tests/JwtAuthenticationTests.cs; обновить JwtAuthModule.cs.
- [ ] T010 [US2] Адаптировать tests/Sitko.Core.Auth.Tests/RedisAuthTests.cs и RedisClusterTests.cs к Duende 4, проверить на изолированном Redis/Sentinel/Cluster.

## US3: инструкция перехода
Независимая проверка: таблица всех прежних имён и точные примеры API/конфигурации.
- [x] T011 [US3] Подготовить docs/ru/oidc-authentication.md и docs/en/oidc-authentication.md: конфигурация, Google/IdentityServer, client role Keycloak, SSO/внешний IdP, logout и Blazor.
- [x] T012 [US3] Обновить ссылки docs/migration-v13.md, docs/migration-v13.en.md и docs/redis-sentinel.md.

## Завершение
- [ ] T013 Проверить сборку/упаковку src/Sitko.Core.Auth.OIDC, отчёт тестов tests/Sitko.Core.Auth.Tests и все новые диагностики C#.
- [ ] T014 Провести независимое ревью, сопоставить реализацию со specs/20261028-102000-oidc-authentication/spec.md и удалить рабочие материалы после переноса результата.
- [ ] T015 Отправить связанный коммит/PR в v13, проверить CI и отсутствие блокеров, слить по подтверждённому поручению и проверить результат.

## Порядок и стратегия
T001–T004 предшествуют пользовательским сценариям. US1 и US2 используют общие токены и выполняются последовательно короткими циклами тест → реализация → проверка. US3 следует за стабилизацией API. T013–T015 завершают все сценарии. Самостоятельная промежуточная поставка US1 не делается: согласована единая миграция. Параллельная реализация не используется; независимое ревью не меняет файлы.
