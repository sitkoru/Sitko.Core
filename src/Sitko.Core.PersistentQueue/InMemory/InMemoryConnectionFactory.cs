using System.Threading.Tasks;

namespace Sitko.Core.PersistentQueue.InMemory
{
    public class InMemoryConnectionFactory : IPersistentQueueConnectionFactory<InMemoryQueueConnection>
    {
        private InMemoryQueueConnection _connection = new InMemoryQueueConnection();

        public Task<InMemoryQueueConnection> GetConnection()
        {
            return Task.FromResult(_connection);
        }

        public InMemoryQueueConnection[] GetCurrentConnections()
        {
            return new[] {_connection};
        }

        public void Dispose()
        {
            _connection = null;
        }
    }
}
