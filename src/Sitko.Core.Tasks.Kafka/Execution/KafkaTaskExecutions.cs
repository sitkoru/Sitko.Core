using Microsoft.Extensions.Hosting;

namespace Sitko.Core.Tasks.Kafka.Execution
{
    public sealed class KafkaTaskExecutions : IHostedLifecycleService, IDisposable
    {
        private readonly Lock sync = new();
        private readonly CancellationTokenSource stopping = new();
        private readonly CancellationTokenSource completion = new();
        private readonly HashSet<Task> active = [];
        private bool accepting = true;
        private CancellationTokenRegistration deadlineRegistration;

        public CancellationToken StoppingToken => stopping.Token;
        public CancellationToken CompletionToken => completion.Token;

        public bool TryRegister(Task execution)
        {
            lock (sync)
            {
                return accepting && active.Add(execution);
            }
        }

        public void Complete(Task execution)
        {
            lock (sync)
            {
                _ = active.Remove(execution);
            }
        }

        public Task StoppingAsync(CancellationToken cancellationToken)
        {
            lock (sync)
            {
                accepting = false;
            }

            deadlineRegistration = cancellationToken.Register(completion.Cancel);
            return stopping.CancelAsync().WaitAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Task[] executions;
            lock (sync)
            {
                executions = [.. active];
            }

            await Task.WhenAll(executions).WaitAsync(cancellationToken);
        }

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public void Dispose()
        {
            deadlineRegistration.Dispose();
            stopping.Dispose();
            completion.Dispose();
        }
    }
}
