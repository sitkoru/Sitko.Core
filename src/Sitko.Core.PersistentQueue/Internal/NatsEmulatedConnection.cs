using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using NATS.Client;

namespace Sitko.Core.PersistentQueue.Internal
{
    [SuppressMessage("ReSharper", "AvoidAsyncSuffix")]
    [SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty")]
    internal class NatsEmulatedConnection : IConnection
    {
        public void Dispose()
        {
        }

        public void Publish(string subject, byte[] data)
        {
        }

        public void Publish(string subject, byte[] data, int offset, int count)
        {
        }

        public void Publish(Msg msg)
        {
        }

        public void Publish(string subject, string reply, byte[] data)
        {
        }

        public void Publish(string subject, string reply, byte[] data, int offset, int count)
        {
        }

        public Msg Request(string subject, byte[] data, int timeout)
        {
            throw new NotImplementedException();
        }

        public Msg Request(string subject, byte[] data, int offset, int count, int timeout)
        {
            throw new NotImplementedException();
        }

        public Msg Request(string subject, byte[] data)
        {
            throw new NotImplementedException();
        }

        public Msg Request(string subject, byte[] data, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public Task<Msg> RequestAsync(string subject, byte[] data, int timeout)
        {
            throw new NotImplementedException();
        }

        public Task<Msg> RequestAsync(string subject, byte[] data, int offset, int count, int timeout)
        {
            throw new NotImplementedException();
        }

        public Task<Msg> RequestAsync(string subject, byte[] data)
        {
            throw new NotImplementedException();
        }

        public Task<Msg> RequestAsync(string subject, byte[] data, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public Task<Msg> RequestAsync(string subject, byte[] data, int timeout, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task<Msg> RequestAsync(string subject, byte[] data, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task<Msg> RequestAsync(string subject, byte[] data, int offset, int count, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public string NewInbox()
        {
            return string.Empty;
        }

        public ISyncSubscription SubscribeSync(string subject)
        {
            throw new NotImplementedException();
        }

        public IAsyncSubscription SubscribeAsync(string subject)
        {
            throw new NotImplementedException();
        }

        public IAsyncSubscription SubscribeAsync(string subject, EventHandler<MsgHandlerEventArgs> handler)
        {
            throw new NotImplementedException();
        }

        public ISyncSubscription SubscribeSync(string subject, string queue)
        {
            throw new NotImplementedException();
        }

        public IAsyncSubscription SubscribeAsync(string subject, string queue)
        {
            throw new NotImplementedException();
        }

        public IAsyncSubscription SubscribeAsync(string subject, string queue,
            EventHandler<MsgHandlerEventArgs> handler)
        {
            throw new NotImplementedException();
        }

        public void Flush(int timeout)
        {
        }

        public void Flush()
        {
        }

        public void Close()
        {
        }

        public bool IsClosed()
        {
            return false;
        }

        public bool IsReconnecting()
        {
            return false;
        }

        public void ResetStats()
        {
        }

        public Task DrainAsync()
        {
            throw new NotImplementedException();
        }

        public Task DrainAsync(int timeout)
        {
            throw new NotImplementedException();
        }

        public void Drain()
        {
            throw new NotImplementedException();
        }

        public void Drain(int timeout)
        {
            throw new NotImplementedException();
        }

        public bool IsDraining()
        {
            throw new NotImplementedException();
        }

        public Options Opts { get; }
        public string ConnectedUrl { get; }
        public string ConnectedId { get; }
        public string[] Servers { get; }
        public string[] DiscoveredServers { get; }
        public Exception LastError { get; }
        public ConnState State { get; }
        public IStatistics Stats { get; }
        public long MaxPayload { get; }
        public int SubscriptionCount { get; }
    }
}
