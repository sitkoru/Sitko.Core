using Sitko.Core.Repository.Tests.Data;
using Sitko.Core.Xunit;

namespace Sitko.Core.Repository.EntityFrameworkCore.Tests.Data;

public class MultipleDbContextsTestScope : EFTestScope
{
    protected override TestApplication ConfigureApplication(TestApplication application, string name)
    {
        base.ConfigureApplication(application, name);
        AddDbContext<SecondTestDbContext>(application, name);
        application.AddEFRepositories<MultipleDbContextsTestScope>();
        return application;
    }
}
