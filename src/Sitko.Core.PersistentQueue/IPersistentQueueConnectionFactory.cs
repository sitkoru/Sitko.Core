using System;
using System.Threading.Tasks;

namespace Sitko.Core.PersistentQueue
{
    public interface IPersistentQueueConnectionFactory<TConnection> : IDisposable
        where TConnection : IPersistentQueueConnection
    {
        Task<TConnection> GetConnection();
        TConnection[] GetCurrentConnections();
    }
}
