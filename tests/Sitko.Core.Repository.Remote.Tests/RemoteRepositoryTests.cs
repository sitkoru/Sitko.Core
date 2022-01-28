using JetBrains.Annotations;
using Sitko.Core.Repository.Tests;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Repository.Remote.Tests;

public class RemoteRepositoryTests : BaseTest<>
{
    public RemoteRepositoryTests([NotNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }
}
