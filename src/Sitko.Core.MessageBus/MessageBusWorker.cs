using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.MessageBus
{
    public class MessageBusWorker
    {
        private readonly ChannelReader<IMessage> _reader;
        private readonly Func<IMessage, Task> _process;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Guid _id = Guid.NewGuid();

        public MessageBusWorker(ChannelReader<IMessage> reader, Func<IMessage, Task> process, ILogger logger)
        {
            _reader = reader;
            _process = process;
            _logger = logger;
        }

        public void Run()
        {
            _logger.LogInformation("Start message bus worker {id}", _id);
            Task.Run(async () =>
            {
                _logger.LogInformation("Message bus worker {id} processing messages", _id);
                await foreach (var message in _reader.ReadAllAsync(_cts.Token))
                {
                    _logger.LogDebug("Message bus worker {id} got new message {message}", _id, message);
                    await _process(message);
                    _logger.LogDebug("Message {message} processed by message bus worker {id}", message, _id);
                }
                _logger.LogInformation("Message bus worker {id} stop processing messages", _id);
            }, _cts.Token);
            _logger.LogInformation("Message bus worker {id} started", _id);
        }

        public void Stop()
        {
            _logger.LogInformation("Stop message bus worker {id}", _id);
            _cts.Cancel();
            _logger.LogInformation("Message bus worker {id} stopped", _id);
        }
    }
}
