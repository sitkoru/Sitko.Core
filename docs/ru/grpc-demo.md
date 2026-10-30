# Локальное gRPC-демо с Consul

[English](../en/grpc-demo.md)

Приложение [apps/gRPC](../../apps/gRPC/README.md) одновременно размещает сервер
и клиент. HTTPS-запрос `/Test` вызывает настоящий `greet.FooService/Foo` с `Bar=123`
через Microsoft Service Discovery и DNS собственного Consul. Ответ — `Baz 123`.
Прямой адрес клиента и запасной путь в обход Consul не настроены.

## Подготовка

Нужны .NET 10 SDK из [global.json](../../global.json), Docker с Compose,
`curl` и `dig`. Команды выполняются из корня репозитория.

В хранилище `CurrentUser/My` должен быть действующий доверенный сертификат
`localhost` с закрытым ключом, подготовленный стандартным `dotnet dev-certs`.
Проверьте его до запуска:

```bash
dotnet dev-certs https --check --trust
```

Если проверка не проходит, сначала подготовьте сертификат и доверие к нему
для своей ОС. Демо не устанавливает сертификаты и не меняет доверие. Оно загружает
сертификат через `CertificateLoader.LoadFromStoreCert` с `allowInvalid=false`.
Проверка сертификата gRPC-клиентом включена.

[AppWebConfigurationModule](../../src/Sitko.Core.App.Web/AppWebConfigurationModule.cs)
генерирует собственный самоподписанный сертификат для именованных TLS-портов.
Демо предъявляет доверенный сертификат через `ConfigureHttpsDefaults` и
`OnAuthenticate` по [существующему образцу Kestrel](../../tests/Sitko.Core.Grpc.Client.Tests.Host/Program.cs).
Это сохраняет стандартную проверку сертификата клиентом без изменения библиотек.

| Назначение | Адрес |
| --- | --- |
| `/Test`, HTTPS HTTP/1 | `https://localhost:18080/Test` |
| gRPC, TLS HTTP/2 | `localhost:18081` |
| Consul HTTP | `http://127.0.0.1:18500` |
| Consul DNS, TCP и UDP | `127.0.0.1:18600` |

Все слушатели демо привязаны к loopback. Эти порты должны быть свободны.
[compose.yml](../../apps/gRPC/compose.yml) создаёт отдельный проект
`sitko-grpc-demo` с одним Consul в режиме разработки; данные Consul временные.

## Запуск и готовность

Запустите Consul и дождитесь непустого адреса лидера:

```bash
docker compose -f apps/gRPC/compose.yml up -d
curl -fsS http://127.0.0.1:18500/v1/status/leader
```

В другом терминале запустите приложение:

```bash
dotnet run --project apps/gRPC/Sitko.Core.Apps.Grpc/Sitko.Core.Apps.Grpc.csproj --launch-profile https
```

Приложение публикует контракты `greet.Greeter` и `greet.FooService`
с окружением `grpc-demo`, адресом `localhost` и фактическим портом `18081`.
Регистрация выполняется асинхронно: запуск процесса ещё не означает готовность RPC.
Проверьте записи и здоровый DNS SRV для `greet.FooService`:

```bash
curl -fsS http://127.0.0.1:18500/v1/agent/services
dig @127.0.0.1 -p 18600 grpc-e10cea5836abef17d515d08191aa0a5fb094324879d81d95579e6a6af0.service.consul SRV +short
```

В HTTP-ответе должны быть оба контракта в `Meta.contract`, окружение `grpc-demo`,
`Address=localhost` и `Port=18081`. DNS-ответ для Foo должен содержать порт `18081`.
Если запись ещё не появилась, повторите проверку после регистрации.
DNS-имя вычисляется из окружения и полного имени контракта; при изменении этих
значений берите актуальный `Service` из ответа `/v1/agent/services` и добавляйте
`.service.consul`.

После появления записи выполните:

```bash
curl --http1.1 --fail-with-body --max-time 15 -i https://localhost:18080/Test
```

Ожидаются HTTP `200` и тело `Baz 123`. В журнале приложения виден маршрут
`gRPC - /greet.FooService/Foo`: обработчик `/Test` возвращает результат RPC,
а не локальную константу. Параметр `Grpc:Client:Default:DefaultDeadline` ограничивает
RPC десятью секундами. Ошибку TLS нужно устранить настройкой сертификата и доверия;
отключение проверки сертификата не входит в сценарий запуска.

## Проверка обязательного Consul и остановка

Для проверки отсутствия прямого пути остановите приложение через `Ctrl+C`,
затем остановите только Consul этого демо:

```bash
docker compose -f apps/gRPC/compose.yml stop consul
```

Снова запустите приложение той же командой и повторите запрос `/Test`.
Успешного `Baz 123` не будет: свежий клиент не может обнаружить сервер.
Сам процесс может запуститься, поскольку библиотека повторяет регистрацию
в фоне. Такая проверка требует свежего процесса: уже работающий клиент может
сохранять соединение или DNS-результат.

Остановите приложение через `Ctrl+C`. Для повторного успешного запуска
поднимите Consul командой `up -d` выше. Для окончательной очистки демо выполните:

```bash
docker compose -f apps/gRPC/compose.yml down
```

Это удаляет ресурсы проекта `sitko-grpc-demo`. Один процесс с клиентом и сервером
не проверяет восстановление нескольких экземпляров, производительность или
готовность production. Устройство библиотек и дополнительные настройки описаны
в [руководстве gRPC/SD](grpc-service-discovery.md).
