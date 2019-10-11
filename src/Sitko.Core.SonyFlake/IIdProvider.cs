using System.Threading.Tasks;

namespace Sitko.Core.SonyFlake
{
    public interface IIdProvider
    {
        Task<long> NextAsync();
    }
}
