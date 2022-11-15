using Sitko.Core.Storage.Tests;
using Xunit.Abstractions;

namespace Sitko.Core.Storage.S3.Tests;

public class BasicTests : BasicTests<BaseS3StorageTestScope>
{
    public BasicTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }
}

