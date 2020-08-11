using System.Threading.Tasks;

namespace Sitko.Core.IdProvider
{
    public interface IIdProvider
    {
        Task<long> NextAsync();
    }
}
