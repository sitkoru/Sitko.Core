using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Events;
using Sitko.Core.Db.InMemory;
using Sitko.Core.Repository.EntityFrameworkCore;
using Sitko.Core.Repository.Remote.Tests.Server;
using Sitko.Core.Repository.Tests.Data;
using Sitko.Core.Xunit;
using Sitko.Core.Xunit.Web;

namespace Sitko.Core.Repository.Remote.Tests.Data;

public class RemoteRepositoryTestScope : WebTestScope
{
    protected override WebTestApplication ConfigureWebApplication(WebTestApplication application, string name)
    {
        base.ConfigureWebApplication(application, name);
        application.AddInMemoryDatabase<TestDbContext>(options =>
        {
            options.Database = name;
        });
        application.AddEFRepositories(options =>
        {
            options.AddRepository<BarEFRepository>();
            options.AddRepository<TestEFRepository>();
            options.AddRepository<FooEFRepository>();
        });
        return application;
    }

    protected override async Task InitWebApplicationAsync(IServiceProvider hostServices)
    {
        await base.InitWebApplicationAsync(hostServices);
        var dbContext = hostServices.CreateScope().ServiceProvider.GetRequiredService<TestDbContext>();
        //add data
        var testModels = new List<TestModel>
        {
            new() {Id = Guid.NewGuid(), FooId = 1},
            new() {Id = Guid.NewGuid(), FooId = 2},
            new() {Id = Guid.NewGuid(), FooId = 3},
            new() {Id = Guid.NewGuid(), FooId = 4},
            new() {Id = Guid.NewGuid(), FooId = 5},
            new() {Id = Guid.NewGuid(), FooId = 5}
        };
        await dbContext.AddRangeAsync(testModels);

        var barModels = new List<BarModel>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TestId = testModels.First().Id,
                JsonModels = new List<BaseJsonModel> {new JsonModelBar(), new JsonModelFoo()}
            },
            new() {Id = Guid.NewGuid()},
            new() {Id = Guid.NewGuid()},
            new() {Id = Guid.NewGuid()},
            new() {Id = Guid.NewGuid()},
            new() {Id = Guid.NewGuid()},
            new() {Id = Guid.NewGuid()},
            new() {Id = Guid.NewGuid()}
        };
        await dbContext.AddRangeAsync(barModels);

        var fooModels = new[]
        {
            new FooModel {Id = Guid.NewGuid(), BarId = barModels[0].Id, FooText = "123"},
            new FooModel {Id = Guid.NewGuid(), BarId = barModels[1].Id, FooText = "456"},
            new FooModel {Id = Guid.NewGuid(), BarId = barModels[1].Id, FooText = "789"},
            new FooModel {Id = Guid.NewGuid(), BarId = barModels[1].Id, FooText = "012"}
        };
        var bazModels = new List<BazModel>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Baz = "1",
                Bars = barModels.Take(2).ToList(),
                Foos = fooModels.Take(2).ToList()
            },
            new()
            {
                Id = Guid.NewGuid(),
                Baz = "2",
                Bars = barModels.Take(5).ToList(),
                Foos = fooModels.Take(2).ToList()
            },
            new() {Id = Guid.NewGuid(), Baz = "3", Foos = fooModels.Take(2).ToList()},
            new() {Id = Guid.NewGuid(), Baz = "4"},
            new() {Id = Guid.NewGuid(), Baz = "5"},
            new() {Id = Guid.NewGuid(), Baz = "6"}
        };
        await dbContext.Set<BazModel>().AddRangeAsync(bazModels);
        await dbContext.Set<FooModel>().AddRangeAsync(fooModels);
        await dbContext.SaveChangesAsync();
    }

    protected override TestApplication ConfigureApplication(TestApplication application, string name)
    {
        base.ConfigureApplication(application, name);
        application.AddRemoteRepositories(options =>
        {
            options.AddRepository<BarRemoteRepository>();
            options.AddRepository<FooRemoteRepository>();
            options.AddRepository<TestRemoteRepository>();
            options.AddRepositoriesFromAssemblyOf<TestModel>();
        });
        application.AddHttpRepositoryTransport(options =>
        {
            options.RepositoryControllerApiRoute = new Uri(Server.BaseAddress, "http://localhost");
            if (Server is not null)
            {
                options.HttpClientFactory = () =>
                {
                    var client = Server.CreateClient();
                    client.BaseAddress = new Uri(client.BaseAddress!, "/api");
                    return client;
                };
            }
        });
        application.ConfigureLogging((_, configuration) =>
        {
            configuration.MinimumLevel.Override("Sitko.Core.Repository", LogEventLevel.Debug);
        });

        return application;
    }
}
