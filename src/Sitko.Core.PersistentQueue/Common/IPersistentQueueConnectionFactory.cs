using System.Threading.Tasks;
using Google.Protobuf;

namespace Sitko.Core.PersistentQueue.Common
{
    public interface IPersistentQueueConnectionFactory
    {
        Task<PersistentQueueConnector> GetConnectorAsync<T>() where T : IMessage;
        PersistentQueueConnection[] GetConnections();
        PersistentQueueConnector[] GetConnectors();
        void ReleaseConnector(PersistentQueueConnector connector, PersistentQueueConnection connection);
    }
}
