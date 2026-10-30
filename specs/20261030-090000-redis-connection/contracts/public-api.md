# Публичный контракт

`RedisModuleOptions`: `Mode` (Direct/Sentinel), `Configuration`, `SentinelConfiguration`.
Раздел `Redis:Connection`. Прежние поля RedisModuleOptions и AuthOptions.Redis* удалены.
`RedisConnection.Database` возвращает выбранную базу; `RedisConnection.Connection` — общий
`IConnectionMultiplexer`. Потребители не освобождают multiplexer самостоятельно.
`AddRedis` и автоматическое подключение Auth разделяют один singleton в одном IServiceProvider.
Keyed RedisConnection/IDatabase по типу AuthOptions не регистрируются.
