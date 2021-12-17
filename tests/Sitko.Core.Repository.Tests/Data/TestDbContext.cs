using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Sitko.Core.Db.Postgres;

namespace Sitko.Core.Repository.Tests.Data;

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<TestModel> TestModels => Set<TestModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.RegisterJsonEnumerableConversion<BarModel, BaseJsonModel, List<BaseJsonModel>>(model =>
            model.JsonModels, "JsonModels");
    }
}
