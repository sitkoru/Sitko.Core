namespace Sitko.Core.Queue.Kafka.Events;

public interface IBaseEvent
{
    string GetKey();
}
