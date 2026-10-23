# Redis и Valkey Sentinel

`Sitko.Core.Redis` содержит общую логику прямого подключения к Redis-совместимому
серверу, обнаружения основного узла через Valkey Sentinel и освобождения соединений.
`Sitko.Core.Auth` и `Sitko.Core.Auth.IdentityServer` используют этот проект
для Data Protection и хранилища токенов.
Одно соединение с данными используется для ключей ASP.NET Core Data Protection
и хранилища токенов `RedisTokenStore` в рамках одного экземпляра модуля.
В режиме Sentinel контейнер DI владеет обоими соединениями
`ConnectionMultiplexer`: с Sentinel и с данными. Переключением при отказе управляет
`StackExchange.Redis`; прикладной цикл опроса основного узла не нужен.

Соединения создаются при первом обращении через DI. При первоначальном подключении
Sentinel должен быть доступен и знать `RedisServiceName`, иначе подключение
завершается ошибкой. После успешного подключения библиотека обнаруживает смену
основного узла и переподключается; отдельные операции во время переключения могут
завершиться ошибкой. Интеграция не добавляет автоматический повтор произвольных
прикладных операций.

## Использование без Auth

Добавьте ссылку на `Sitko.Core.Redis` и зарегистрируйте модуль:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Redis;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);
builder.AddSitkoCore().AddRedis(options =>
{
    options.SentinelEndpoints = ["sentinel-1.example.net:26379", "sentinel-2.example.net:26379"];
    options.ServiceName = "app-primary";
    options.Db = 3;
});
using var host = builder.Build();
var database = host.Services.GetRequiredService<IDatabase>();
await database.StringSetAsync("example", "value");
```

Настройки `RedisModuleOptions` читаются из секции `Redis`; имя секции можно
переопределить параметром `optionsKey` у `AddRedis`. Для прямого подключения
задайте `Host` и `Port` вместо `SentinelEndpoints` и `ServiceName`.
Названия остальных параметров совпадают с параметрами Auth ниже без префикса
`Redis`: `Db`, `User`, `Password`, `SentinelUser`, `SentinelPassword`.
Общий модуль регистрирует `RedisConnection` и `IDatabase` в DI; соединением
владеет контейнер, вручную освобождать его в потребителе не нужно.

Auth сохраняет собственные секции и прежние плоские параметры `Redis*`.
Дополнительный вызов `AddRedis` для Auth не требуется: он использует общий
`RedisConnection` с настройками соответствующего модуля аутентификации.

## Конфигурация Auth

Пример секции `Auth:IdentityServer:Oidc`:

```json
{
  "Auth": {
    "IdentityServer": {
      "Oidc": {
        "EnableRedisDataProtection": true,
        "TokenStoreType": "Redis",
        "RedisSentinelEndpoints": [
          "sentinel-1.example.net:26379",
          "sentinel-2.example.net:26379",
          "sentinel-3.example.net:26379"
        ],
        "RedisServiceName": "auth-primary",
        "RedisDb": 3,
        "RedisUser": "auth-app",
        "RedisPassword": "example-data-password",
        "RedisSentinelUser": "sentinel-client",
        "RedisSentinelPassword": "example-sentinel-password"
      }
    }
  }
}
```

Пароли здесь вымышлены; реальные значения передавайте через используемый приложением
источник секретов. Остальные обязательные настройки OIDC задаются как обычно.
Имя `RedisServiceName` должно совпадать с именем группы в `sentinel monitor`.
Адреса `RedisSentinelEndpoints` задаются в виде `host:port`.

| Параметр | Назначение |
| --- | --- |
| `RedisSentinelEndpoints` | Начальные адреса Sentinel; задаются вместе с `RedisServiceName`. |
| `RedisServiceName` | Имя группы, основной узел которой обнаруживается через Sentinel. |
| `RedisUser`, `RedisPassword` | Учётные данные для узлов с данными; пользователь необязателен при парольной аутентификации без отдельного пользователя ACL. |
| `RedisSentinelUser`, `RedisSentinelPassword` | Отдельные учётные данные для подключения приложения к Sentinel. |
| `RedisDb` | Номер базы с данными, ключами Data Protection и токенами; к Sentinel не применяется. |
| `RedisHost`, `RedisPort` | Адрес прямого подключения; в режиме Sentinel игнорируется. |

Учётные данные Sentinel **не наследуются** из `RedisUser` или `RedisPassword`.
Если Sentinel требует аутентификацию, задайте её явно. Учётные данные,
с которыми сами процессы Sentinel обращаются к узлам с данными
(`sentinel auth-user` и `sentinel auth-pass`), настраиваются на стороне Sentinel.

Если параметры Sentinel отсутствуют, сохраняется прежнее прямое подключение
через `RedisHost`, `RedisPort`, `RedisPassword` и `RedisDb`.
Новый `RedisUser` также доступен для прямого подключения.
Не задавайте только одну половину конфигурации Sentinel:
нужны и непустой список адресов, и непустое имя группы.

## Требования к развёртыванию

Для рабочей среды разместите как минимум три Sentinel на независимых узлах,
которые не отказывают одновременно. Основному узлу с данными нужны реплики.
Один контейнер тестового стенда не обеспечивает такую отказоустойчивость.

Из приложения должны быть доступны все адреса основного узла, реплик и Sentinel,
которые объявляются через обнаружение, включая адреса после переключения.
Публикации только начальных адресов Sentinel недостаточно.
При NAT и отображении портов проверьте объявляемые адреса и порты:
адрес, доступный Sentinel внутри сети, может быть недоступен приложению.
Подробности: [документация Valkey Sentinel](https://valkey.io/topics/sentinel/).

Ключи Data Protection требуют постоянного хранения: настройте сохранение данных
Valkey на диск и политику `maxmemory-policy noeviction`.
Потеря ключей мешает расшифровке ранее выданных защищённых данных.
Для экземпляров одного приложения сохраняйте одинаковые `ApplicationName` и
`RedisDb`. Имя ключа остаётся `{ApplicationName}-DP`, а имя приложения
для Data Protection не меняется. Общая база сама по себе не заменяет совпадение
имени приложения.

Sentinel использует асинхронную репликацию и не гарантирует сохранение каждой
подтверждённой записи при отказе. Постоянное хранение и `noeviction` не устраняют
это ограничение. Выбирайте требования к допустимой потере данных с учётом
[гарантий Valkey Sentinel](https://valkey.io/topics/sentinel/).

Подключение использует существующую зависимость `StackExchange.Redis` версии
`2.7.27`; параметры обнаружения описаны в
[документации этой версии](https://github.com/StackExchange/StackExchange.Redis/blob/2.7.27/docs/Configuration.md).

## Локальная проверка

Нужны Docker с Docker Compose, .NET SDK, выбранный `global.json` (10.0.201
с допустимым переходом на более новый набор компонентов SDK), и среда выполнения
.NET 8 для запуска тестов `net8.0`. Команды выполняются из корня репозитория.
Порты `16379–16381` и `26379–26381` на `127.0.0.1` должны быть свободны.

Стенд `tests/Sitko.Core.Auth.Tests/Valkey/compose.yml` запускает в одном контейнере
три узла Valkey и три Sentinel. Образ `valkey/valkey:8.1.4-alpine` закреплён
по digest. Адреса и порты внутри контейнера совпадают с опубликованными;
тесты запускаются на машине Docker, а не в соседнем контейнере.
Пароли стенда являются только тестовыми данными. Сохранение на диск в нём
отключено, поэтому его конфигурация не подходит для рабочей среды.

```sh
docker compose -p sitko-sentinel-tests -f tests/Sitko.Core.Auth.Tests/Valkey/compose.yml up -d --wait --force-recreate
dotnet test tests/Sitko.Core.Auth.Tests/Sitko.Core.Auth.Tests.csproj --framework net8.0 --logger "trx;LogFileName=sentinel.trx" --results-directory TestResults/sentinel-net8.0

docker compose -p sitko-sentinel-tests -f tests/Sitko.Core.Auth.Tests/Valkey/compose.yml up -d --wait --force-recreate
dotnet test tests/Sitko.Core.Auth.Tests/Sitko.Core.Auth.Tests.csproj --framework net10.0 --logger "trx;LogFileName=sentinel.trx" --results-directory TestResults/sentinel-net10.0
```

Каждая целевая платформа требует свежего стенда: тест переключения отключает
исходный основной узел. Перед повторным запуском также пересоздайте контейнер.
Тесты обязательны и завершаются ошибкой при недоступном стенде.

Проверка охватывает прямое подключение, отдельную аутентификацию Sentinel,
общие данные между экземплярами приложения и переключение при отказе.
Перед отказом тест дожидается репликации записей; после него проверяет
сохранённые данные также через новый экземпляр приложения и контейнера DI.
Это подтверждает проверяемый сценарий, но не отсутствие потерь произвольных
конкурентных записей в рабочей среде.

После проверки сохраните журналы при необходимости и остановите стенд,
в том числе если тесты завершились ошибкой:

```sh
docker compose -p sitko-sentinel-tests -f tests/Sitko.Core.Auth.Tests/Valkey/compose.yml logs --no-color
docker compose -p sitko-sentinel-tests -f tests/Sitko.Core.Auth.Tests/Valkey/compose.yml down --volumes
```

В CI `.github/workflows/sentinel.yml` выполняет эти тесты отдельно для `net8.0`
и `net10.0`, сохраняет TRX и всегда запрашивает журналы и удаление стенда.
Эта настройка не мигрирует существующие данные или ключи, не изменяет рабочие
приложения и не добавляет поддержку Redis Cluster.
