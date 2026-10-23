using StackExchange.Redis;

namespace Sitko.Core.Auth
{
    internal sealed class AuthRedisConnection<TOptions> : IDisposable where TOptions : AuthOptions
    {
        private readonly ConnectionMultiplexer connection;
        private readonly ConnectionMultiplexer? sentinel;
        private readonly int database;

        public AuthRedisConnection(TOptions options)
        {
            database = options.RedisDb;
            var redisOptions = new ConfigurationOptions
            {
                User = options.RedisUser,
                Password = options.RedisPassword
            };
            if (!options.UseRedisSentinel)
            {
                redisOptions.EndPoints.Add(options.RedisHost!, options.RedisPort);
                connection = ConnectionMultiplexer.Connect(redisOptions);
                return;
            }

            var sentinelOptions = new ConfigurationOptions
            {
                User = options.RedisSentinelUser,
                Password = options.RedisSentinelPassword
            };
            foreach (var endpoint in options.RedisSentinelEndpoints)
            {
                sentinelOptions.EndPoints.Add(endpoint);
            }

            sentinel = ConnectionMultiplexer.SentinelConnect(sentinelOptions);
            try
            {
                redisOptions.ServiceName = options.RedisServiceName;
                connection = sentinel.GetSentinelMasterConnection(redisOptions);
            }
            catch
            {
                sentinel.Dispose();
                throw;
            }
        }

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
