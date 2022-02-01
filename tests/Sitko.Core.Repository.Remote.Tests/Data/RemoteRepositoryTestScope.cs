﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog.Events;
using Sitko.Core.Db.InMemory;
using Sitko.Core.Db.Postgres;
using Sitko.Core.Repository.Tests.Data;
using Sitko.Core.Xunit;
using Sitko.Core.Xunit.Web;

namespace Sitko.Core.Repository.Remote.Tests.Data;

public class RemoteRepositoryTestScope : WebTestScope
{
    protected override WebTestApplication ConfigureWebApplication(WebTestApplication application, string name)
    {
        base.ConfigureWebApplication(application, name);
        application.AddInMemoryDatabase<TestDbContext>();
        //application.add
        return application;
    }

    protected override TestApplication ConfigureApplication(TestApplication application, string name)
    {
        base.ConfigureApplication(application, name);
        application.AddRemoteRepositories(options =>
        {
            options.AddRepository<BarRepository>();
            options.AddRepository<FooRepository>();
            options.AddRepository<BazRepository>();
        });
        application.ConfigureLogging((_, configuration) =>
        {
            configuration.MinimumLevel.Override("Sitko.Core.Repository", LogEventLevel.Debug);
        });

        return application;
    }

    // protected override async Task InitDbContextAsync(TestDbContext dbContext)
    // {
    //     await base.InitDbContextAsync(dbContext);
    //     var testModels = new List<TestModel>
    //     {
    //         new() { Id = Guid.NewGuid(), FooId = 1 },
    //         new() { Id = Guid.NewGuid(), FooId = 2 },
    //         new() { Id = Guid.NewGuid(), FooId = 3 },
    //         new() { Id = Guid.NewGuid(), FooId = 4 },
    //         new() { Id = Guid.NewGuid(), FooId = 5 },
    //         new() { Id = Guid.NewGuid(), FooId = 5 }
    //     };
    //     await dbContext.AddRangeAsync(testModels);
    //
    //     var barModels = new List<BarModel>
    //     {
    //         new()
    //         {
    //             Id = Guid.NewGuid(),
    //             TestId = testModels.First().Id,
    //             JsonModels = new List<BaseJsonModel> { new JsonModelBar(), new JsonModelFoo() }
    //         },
    //         new() { Id = Guid.NewGuid() },
    //         new() { Id = Guid.NewGuid() },
    //         new() { Id = Guid.NewGuid() },
    //         new() { Id = Guid.NewGuid() },
    //         new() { Id = Guid.NewGuid() },
    //         new() { Id = Guid.NewGuid() },
    //         new() { Id = Guid.NewGuid() }
    //     };
    //     await dbContext.AddRangeAsync(barModels);
    //
    //     var fooModels = new[]
    //     {
    //         new FooModel { Id = Guid.NewGuid(), BarId = barModels[0].Id, FooText = "123" },
    //         new FooModel { Id = Guid.NewGuid(), BarId = barModels[1].Id, FooText = "456" },
    //         new FooModel { Id = Guid.NewGuid(), BarId = barModels[1].Id, FooText = "789" },
    //         new FooModel { Id = Guid.NewGuid(), BarId = barModels[1].Id, FooText = "012" }
    //     };
    //     var bazModels = new List<BazModel>
    //     {
    //         new()
    //         {
    //             Id = Guid.NewGuid(),
    //             Baz = "1",
    //             Bars = barModels.Take(2).ToList(),
    //             Foos = fooModels.Take(2).ToList()
    //         },
    //         new()
    //         {
    //             Id = Guid.NewGuid(),
    //             Baz = "2",
    //             Bars = barModels.Take(5).ToList(),
    //             Foos = fooModels.Take(2).ToList()
    //         },
    //         new() { Id = Guid.NewGuid(), Baz = "3", Foos = fooModels.Take(2).ToList() },
    //         new() { Id = Guid.NewGuid(), Baz = "4" },
    //         new() { Id = Guid.NewGuid(), Baz = "5" },
    //         new() { Id = Guid.NewGuid(), Baz = "6" }
    //     };
    //     await dbContext.Set<BazModel>().AddRangeAsync(bazModels);
    //     await dbContext.Set<FooModel>().AddRangeAsync(fooModels);
    //     await dbContext.SaveChangesAsync();
    // }
}
