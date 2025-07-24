using Sitko.Core.Storage.Tests;
using Xunit;

namespace Sitko.Core.Storage.Remote.Tests;

public class BasicTests : BasicTests<BaseRemoteStorageTestScope>
{
    public BasicTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }
}
