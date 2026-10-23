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
            database = options.Db;
            var redisOptions = new ConfigurationOptions
            {
                User = options.User,
                Password = options.Password
            };
            if (!options.UseSentinel)
            {
                redisOptions.EndPoints.Add(options.Host!, options.Port);
                connection = ConnectionMultiplexer.Connect(redisOptions);
                return;
            }

            var sentinelOptions = new ConfigurationOptions
            {
                User = options.SentinelUser,
                Password = options.SentinelPassword
            };
            foreach (var endpoint in options.SentinelEndpoints)
            {
                sentinelOptions.EndPoints.Add(endpoint);
            }

            sentinel = ConnectionMultiplexer.SentinelConnect(sentinelOptions);
            try
            {
                redisOptions.ServiceName = options.ServiceName;
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
