using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Infrastructure.Db
{
    // ReSharper disable once UnusedTypeParameter
    public abstract class BaseDbModule<TDbContext, TConfig> : BaseApplicationModule<TConfig>
        where TDbContext : DbContext
        where TConfig : BaseDbModuleConfig
    {
    }

    public abstract class BaseDbModuleConfig
    {
        public BaseDbModuleConfig(string database)
        {
            Database = database;
        }

        public string Database { get; }

        public Action<DbContextOptionsBuilder, IServiceProvider, IConfiguration, IHostEnvironment> Configure
        {
            get;
            set;
        }
    }
}
