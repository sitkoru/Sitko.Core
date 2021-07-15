using Sitko.Core.Storage.Tests;
using Xunit.Abstractions;

namespace Sitko.Core.Storage.FileSystem.Tests
{
    public class BasicTests : BasicTests<BaseFileSystemStorageTestScope>
    {
        public BasicTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
    }
}
