using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Sitko.Core.Db.InMemory
{
    public class InMemoryDatabaseModuleConfig<TDbContext> : BaseDbModuleConfig<TDbContext> where TDbContext : DbContext
    {
        public override (bool isSuccess, IEnumerable<string> errors) CheckConfig()
        {
            var result = base.CheckConfig();
            if (result.isSuccess)
            {
                if (string.IsNullOrEmpty(Database))
                {
                    return (false, new[] {"Empty inmemory database name"});
                }
            }

            return result;
        }
    }
}
