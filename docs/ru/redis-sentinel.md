# Redis, Valkey Sentinel и Cluster

[English](../en/redis-sentinel.md)

`Sitko.Core.Redis` содержит общую логику прямого подключения к Redis-совместимому
серверу, обнаружения основного узла через Valkey Sentinel и освобождения соединений.
`Sitko.Core.Auth` и `Sitko.Core.Auth.OIDC` используют этот проект
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

Пример секции `Auth:OIDC:Client`:

```json
{
  "Auth": {
    "OIDC": {
      "Client": {
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

## Valkey Cluster и несколько баз

Для подключения к Cluster задайте начальный узел через `Host` и `Port` общего
модуля либо `RedisHost` и `RedisPort` Auth. `ServiceName` / `RedisServiceName`
и списки адресов Sentinel должны отсутствовать; пустая строка имени сервиса
не отключает режим Sentinel. Клиент обнаруживает остальные узлы автоматически.
Из приложения должны быть доступны все объявленные кластером адреса.
После смены основного узла открытым соединениям требуется время на обновление
маршрутов. В этот период отдельные операции могут завершаться ошибкой; модуль
не добавляет автоматический повтор прикладных операций.

Номер базы задаётся через `Db` / `RedisDb`. Для баз, отличных от DB 0, требуется
Valkey версии 9.0 или новее и параметр `cluster-databases` на узлах сервера.
Например, `cluster-databases 4` разрешает DB 0–3; по умолчанию доступна только DB 0.
Настройка `databases` в режиме Cluster не применяется.
Операции с несколькими ключами требуют одного hash slot; выбор базы это
ограничение не снимает. Подробности:
[спецификация Valkey Cluster](https://valkey.io/topics/cluster-spec/).

Для раздельных окружений назначайте разные базы, если имена приложений совпадают.
Номер базы разделяет ключи с одинаковыми именами. Имена ключей Data Protection
и `ApplicationName` при переключении сервера и переносе кольца нужно сохранять.
Хранилище пользовательских токенов формирует ключ как SHA256 от
`DOT_{ApplicationName}_{Environment}_{userId}`: приложение и окружение уже входят
в идентичность ключа.

## Требования к развёртыванию

При использовании Sentinel в рабочей среде разместите как минимум три Sentinel
на независимых узлах, которые не отказывают одновременно. Основному узлу
с данными нужны реплики.
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
`RedisDb`. Имя ключа — `{ApplicationName}-DP`, имя приложения
для Data Protection — `ApplicationName`. Общая база сама по себе не заменяет совпадение
имени приложения.

Sentinel использует асинхронную репликацию и не гарантирует сохранение каждой
подтверждённой записи при отказе. Постоянное хранение и `noeviction` не устраняют
это ограничение. Выбирайте требования к допустимой потере данных с учётом
[гарантий Valkey Sentinel](https://valkey.io/topics/sentinel/).

Подключение использует `StackExchange.Redis` версии `3.4.20`, включая протокол
RESP3 по умолчанию. Параметры обнаружения описаны в
[документации этой версии](https://github.com/StackExchange/StackExchange.Redis/blob/3.4.20/docs/Configuration.md).

## Локальная проверка

Нужны Docker с Docker Compose, стабильный SDK .NET 10, выбранный `global.json`,
и среда выполнения .NET 10. Команды выполняются из корня репозитория.
Порты `16379–16381`, `17379–17384` и `26379–26381` на `127.0.0.1` должны быть свободны.

Стенд `tests/Sitko.Core.Auth.Tests/Valkey/compose.yml` запускает два контейнера:
три узла Valkey и три Sentinel на `valkey/valkey:8.1.4-alpine`, а также Cluster
из трёх основных узлов и трёх реплик на `valkey/valkey:9.0.3-alpine`.
Образы закреплены по digest. В Cluster доступны DB 0–3. Адреса и порты внутри
контейнеров совпадают с опубликованными;
тесты запускаются на машине Docker, а не в соседнем контейнере.
Пароли стенда являются только тестовыми данными. Сохранение на диск в нём
отключено, поэтому его конфигурация не подходит для рабочей среды.

```sh
docker compose -p sitko-sentinel-tests -f tests/Sitko.Core.Auth.Tests/Valkey/compose.yml up -d --wait --force-recreate
dotnet test --project tests/Sitko.Core.Auth.Tests/Sitko.Core.Auth.Tests.csproj --framework net10.0 --report-trx --report-trx-filename sentinel.trx --results-directory TestResults/sentinel-net10.0
```

Каждый запуск требует свежего стенда: тест переключения отключает
исходный основной узел Sentinel, а тест Cluster переключает основной узел
на реплику. Перед повторным запуском также пересоздайте контейнеры.
Тесты обязательны и завершаются ошибкой при недоступном стенде.

Проверка охватывает прямое подключение, отдельную аутентификацию Sentinel,
общие данные между экземплярами приложения и переключение при отказе.
Проверка Cluster охватывает изоляцию DB 0 и DB 3, Data Protection, пользовательские
токены и восстановление чтения и записи на исходных соединениях после
переключения основного узла.
В сценарии Sentinel тест дожидается репликации записей перед остановкой
основного узла. На тестовом стенде Sentinel считает узел недоступным через
пять секунд: это даёт запас относительно штатного интервала PING в одну секунду
при переподключении после смены роли. В Cluster тест последовательно выполняет
управляемое переключение каждого основного узла на реплику. Оба сценария проверяют сохранённые данные
также через новый экземпляр приложения и контейнера DI.
После переключения тесты ждут восстановления каждой изменяющей операции
на исходном соединении в пределах штатного интервала проверки ролей клиента
с запасом 30 секунд. После успешной операции проверяется только чтение результата.
Это подтверждает проверяемый сценарий, но не отсутствие потерь произвольных
конкурентных записей в рабочей среде.

После проверки сохраните журналы при необходимости и остановите стенд,
в том числе если тесты завершились ошибкой:

```sh
docker compose -p sitko-sentinel-tests -f tests/Sitko.Core.Auth.Tests/Valkey/compose.yml logs --no-color
docker compose -p sitko-sentinel-tests -f tests/Sitko.Core.Auth.Tests/Valkey/compose.yml down --volumes
```

В CI задание `redis` в `.github/workflows/main.yml` последовательно выполняет
тесты для `net10.0` в общем образе `actions-container`. Каждый запуск
получает новые контейнеры Valkey и Sentinel. Они используют сетевое пространство
контейнера задания, чтобы объявленные адреса `127.0.0.1` были доступны тестам.
Задание сохраняет TRX в артефакт `redis-net10.0-test-results` и всегда запрашивает журналы
и удаление стенда.
Локальный стенд и CI не проверяют Kubernetes-оператор, зональные диски
или перенос существующих данных между серверами.

Источники: [соединение](../../src/Sitko.Core.Redis/RedisConnection.cs),
[параметры](../../src/Sitko.Core.Redis/RedisModuleOptions.cs),
[версии зависимостей](../../Directory.Packages.props).
