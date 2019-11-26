using System.Threading.Tasks;

namespace Sitko.Core.PersistentQueue.InMemory
{
    public class InMemoryPersistentQueueMessage : PersistentQueueMessage
    {
        public InMemoryPersistentQueueMessage(byte[] data, string replyTo = null) : base(data, replyTo)
        {
        }

        public override Task Ack()
        {
            return Task.CompletedTask;
        }
    }
}