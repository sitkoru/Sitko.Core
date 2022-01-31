using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using JetBrains.Annotations;
using Sitko.Core.Repository.EntityFrameworkCore.Tests.Data;
using Sitko.Core.Repository.Tests;
using Sitko.Core.Repository.Tests.Data;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Repository.EntityFrameworkCore.Tests;

public class EFTests : BasicRepositoryTests<EFTestScope>
{
    public EFTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task MultipleDbContexts()
    {
        var scope = await GetScopeAsync<MultipleDbContextsTestScope>();
        var barRepository = scope.GetService<IRepository<BarModel, Guid>>();
        var fooBarRepository = scope.GetService<IRepository<FooBarModel, Guid>>();

        var bars = (await barRepository.GetAllAsync()).items;
        bars.Should().NotBeEmpty();

        var fooBar = new FooBarModel { Id = Guid.NewGuid(), BarId = bars.First().Id };
        var res = await fooBarRepository.AddAsync(fooBar);
        res.IsSuccess.Should().BeTrue();
    }
}
