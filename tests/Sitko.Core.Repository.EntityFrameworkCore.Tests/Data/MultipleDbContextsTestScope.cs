using Microsoft.Extensions.Hosting;
using Sitko.Core.Repository.Tests.Data;

namespace Sitko.Core.Repository.EntityFrameworkCore.Tests.Data;

public class MultipleDbContextsTestScope : EFTestScope
{
    protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name)
    {
        base.ConfigureApplication(hostBuilder, name);
        AddDbContext<SecondTestDbContext>(hostBuilder, name, configurePostgres: ConfigurePostgresDatabaseModule);
        hostBuilder.AddEFRepositories<MultipleDbContextsTestScope>();
        return hostBuilder;
    }
}
