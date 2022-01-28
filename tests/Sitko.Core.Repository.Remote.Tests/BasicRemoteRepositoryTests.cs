using JetBrains.Annotations;
using Sitko.Core.Repository.Tests;
using Xunit.Abstractions;

namespace Sitko.Core.Repository.Remote.Tests;

public class BasicRemoteRepositoryTests : BasicRepositoryTests<>
{
    public BasicRemoteRepositoryTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }
}
