using StackExchange.Redis;

namespace Sitko.Core.Redis
{
    public sealed class RedisConnection : IDisposable
    {
        private readonly ConnectionMultiplexer connection;
        private readonly ConnectionMultiplexer? sentinel;
        private readonly int database;

        public RedisConnection(RedisModuleOptions options)
        {
            var (redisOptions, sentinelOptions) = options.ParseConfiguration();
            database = redisOptions.DefaultDatabase ?? -1;
            if (sentinelOptions is null)
            {
                connection = ConnectionMultiplexer.Connect(redisOptions);
                return;
            }

            sentinel = ConnectionMultiplexer.SentinelConnect(sentinelOptions);
            try
            {
                connection = sentinel.GetSentinelMasterConnection(redisOptions);
            }
            catch
            {
                sentinel.Dispose();
                throw;
            }
        }

        public IConnectionMultiplexer Connection => connection;

        public IDatabase Database => connection.GetDatabase(database);

        public void Dispose()
        {
            try
            {
                connection.Dispose();
            }
            finally
            {
                sentinel?.Dispose();
            }
        }
    }
}
