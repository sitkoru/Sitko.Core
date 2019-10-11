using System.Threading.Tasks;
using Google.Protobuf;

namespace Sitko.Core.PersistentQueue.Common
{
    public abstract class PersistentQueueChannel
    {
        private readonly IPersistentQueueConnectionFactory _connectionFactory;

        protected PersistentQueueChannel(IPersistentQueueConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        protected async Task<PersistentQueueConnector> GetConnectorAsync<T>() where T : IMessage
        {
            return await _connectionFactory.GetConnectorAsync<T>();
        }
    }
}
