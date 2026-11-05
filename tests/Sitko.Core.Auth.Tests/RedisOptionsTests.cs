using Sitko.Core.Redis;
using Xunit;

namespace Sitko.Core.Auth.Tests
{
    public sealed class RedisOptionsTests
    {
        [Fact]
        public void SentinelDoesNotRequireDirectHost()
        {
            var options = new RedisModuleOptions
            {
                Mode = RedisConnectionMode.Sentinel,
                Configuration = "serviceName=auth,user=app,password=data-test,defaultDatabase=3",
                SentinelConfiguration = "127.0.0.1:26379,sentinel.example:26379,user=sentinel,password=sentinel-test"
            };
            Assert.True(new RedisModuleOptionsValidator().Validate(options).IsValid);
        }

        [Theory]
        [InlineData("127.0.0.1:6379", null)]
        [InlineData("serviceName=auth", null)]
        [InlineData("serviceName=auth", "")]
        [InlineData("serviceName=auth", "password=sentinel-test")]
        public void IncompleteSentinelConfigurationIsRejected(string configuration, string? sentinelConfiguration)
        {
            var options = new RedisModuleOptions
            {
                Mode = RedisConnectionMode.Sentinel,
                Configuration = configuration,
                SentinelConfiguration = sentinelConfiguration
            };
            Assert.False(new RedisModuleOptionsValidator().Validate(options).IsValid);
        }

        [Theory]
        [InlineData("127.0.0.1:65536")]
        [InlineData("sentinel:invalid")]
        [InlineData("redis://sentinel:26379")]
        public void InvalidSentinelEndpointIsRejected(string endpoint)
        {
            var options = new RedisModuleOptions
            {
                Mode = RedisConnectionMode.Sentinel,
                Configuration = "serviceName=auth",
                SentinelConfiguration = endpoint
            };
            Assert.False(new RedisModuleOptionsValidator().Validate(options).IsValid);
        }
    }
}
