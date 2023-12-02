using Sitko.Core.Repository.Tests.Data;
using Sitko.Core.Xunit;

namespace Sitko.Core.Repository.EntityFrameworkCore.Tests.Data;

public abstract class BaseEFTestScope : DbBaseTestScope<TestDbContext>;
