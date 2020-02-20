using System.Buffers;
using System.IO;
using System.Threading.Tasks;

namespace Sitko.Core.Storage.Cache
{
    public class InMemoryStorageCacheRecord : StorageCacheRecord
    {
        public InMemoryStorageCacheRecord(StorageItem item, IMemoryOwner<byte>? data = null) : base(item)
        {
            Data = data;
        }

        public void SetData(IMemoryOwner<byte> data)
        {
            Data = data;
        }

        public IMemoryOwner<byte>? Data { get; private set; }

        public async Task<Stream?> GetStreamAsync()
        {
            if (Data == null)
            {
                return null;
            }

            
#if NETSTANDARD2_1
            var stream = new MemoryStream();
            await stream.WriteAsync(Data.Memory);
            stream.Seek(0, SeekOrigin.Begin);
            stream.SetLength(Item.FileSize);
#else
            var stream = new MemoryStream(Data.Memory.Span.ToArray());
#endif

            return stream;
        }
    }
}
