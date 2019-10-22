using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Db
{
    public abstract class BaseDbModule<TDbContext, TConfig> : BaseApplicationModule<TConfig>
        where TDbContext : DbContext
        where TConfig : BaseDbModuleConfig<TDbContext>
    {
    }

    public abstract class BaseDbModuleConfig<TDbContext> where TDbContext : DbContext
    {
        public BaseDbModuleConfig(string database)
        {
            Database = database;
        }

        public string Database { get; }

        public Action<DbContextOptionsBuilder<TDbContext>, IServiceProvider, IConfiguration, IHostEnvironment> Configure
        {
            get;
            set;
        }
    }
}
