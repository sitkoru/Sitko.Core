using Microsoft.EntityFrameworkCore;

namespace Sitko.Core.Tasks.Kafka.Tests.Data;

public class TestDbContext : TasksDbContext<BaseTestTask>
{
    public TestDbContext(DbContextOptions options) : base(options)
    {
    }
}
