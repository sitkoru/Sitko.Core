using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Xunit;

namespace Sitko.Core.Repository.Tests
{
    public class SerializationTests
    {
        [Fact]
        public void DeserializeConditionsGroup()
        {
            var json = "[{\"conditions\":[{\"property\":\"projectId\",\"operator\":1,\"value\":1}]}]";

            var where = JsonConvert.DeserializeObject<List<QueryContextConditionsGroup>>(json);

            Assert.NotNull(where);
            Assert.NotEmpty(where);
            Assert.Single(where);
            Assert.Equal("projectId", where.First().Conditions.First().Property);
            Assert.Equal(QueryContextOperator.Equal, where.First().Conditions.First().Operator);
            Assert.Equal(1L, where.First().Conditions.First().Value);
        }
    }
}
