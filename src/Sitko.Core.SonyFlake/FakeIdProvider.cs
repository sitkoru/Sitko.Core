using System.Threading;
using System.Threading.Tasks;

namespace Sitko.Core.SonyFlake
{
    public class FakeIdProvider : IIdProvider
    {
        private long _id = 100500;

        public Task<long> NextAsync()
        {
            Interlocked.Increment(ref _id);
            return Task.FromResult(_id);
        }
    }
}
