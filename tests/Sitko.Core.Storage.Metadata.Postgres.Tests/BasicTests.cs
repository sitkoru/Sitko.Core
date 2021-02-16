using Sitko.Core.Storage.Tests;
using Xunit.Abstractions;

namespace Sitko.Core.Storage.Metadata.Postgres.Tests
{
    public class BasicTests : BasicTests<BasePostgresStorageTestScope, TestS3StorageSettings>
    {
        public BasicTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
    }
}
