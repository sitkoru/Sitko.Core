using System;
using Sitko.Core.Db.Postgres;
using Sitko.Core.Repository.Tests.Data;
using Sitko.Core.Xunit;
using Sitko.Core.Xunit.Web;

namespace Sitko.Core.Repository.Remote.Tests;

public class RemoteRepositoryTestScope : WebTestScope
{
    protected override WebTestApplication ConfigureWebApplication(WebTestApplication application, string name)
    {
        base.ConfigureWebApplication(application, name);
        application.AddPostgresDatabase<TestDbContext>();
        //application.add
        return application;
    }

    protected override TestApplication ConfigureApplication(TestApplication application, string name)
    {
        base.ConfigureApplication(application, name);
        application.AddRemoteRepositories<>();
        return application;
    }
}
