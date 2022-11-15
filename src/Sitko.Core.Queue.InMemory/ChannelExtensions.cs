using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Sitko.Core.Queue.InMemory;

public static class ChannelExtensions
{
    public static async IAsyncEnumerable<T> ReadAllAsync<T>(this ChannelReader<T> reader,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            while (reader.TryRead(out var item))
            {
                yield return item;
            }
        }
    }
}

