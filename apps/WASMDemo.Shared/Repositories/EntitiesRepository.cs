using Sitko.Core.Repository.EntityFrameworkCore;
using WASMDemo.Shared.Data.Models;

namespace WASMDemo.Shared.Repositories
{
    public class EntitiesRepository : EFRepository<TestEntity, Guid, WasmDbContext>
    {
        public EntitiesRepository(EFRepositoryContext<TestEntity, Guid, WasmDbContext> repositoryContext) : base(repositoryContext)
        {
        }
    }
}
