# Проверка

Нужны .NET SDK из global.json и локальный Docker.

```sh
dotnet test --project tests/Sitko.Core.Queue.Kafka.Tests/Sitko.Core.Queue.Kafka.Tests.csproj --report-trx
```

Ожидается успешная проверка прежнего Produce и нового ProduceAsync.
Контракт: [spec.md](spec.md). Никакие производственные брокеры не используются.
