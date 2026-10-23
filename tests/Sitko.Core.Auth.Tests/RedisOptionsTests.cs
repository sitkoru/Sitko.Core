using Sitko.Core.Auth.IdentityServer;
using Sitko.Core.Auth.IdentityServer.Tokens;
using Xunit;

namespace Sitko.Core.Auth.Tests
{
    public sealed class RedisOptionsTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SentinelDoesNotRequireDirectHost(bool tokensOnly)
        {
            var options = CreateOptions(tokensOnly);
            options.RedisServiceName = "auth";
            options.RedisSentinelEndpoints = ["127.0.0.1:26379", "sentinel.example:26379"];
            Assert.True(new OidcAuthOptionsValidator().Validate(options).IsValid);
        }

        [Theory]
        [InlineData(null, "127.0.0.1:26379")]
        [InlineData("", "127.0.0.1:26379")]
        [InlineData(" ", "127.0.0.1:26379")]
        [InlineData("auth", null)]
        [InlineData("auth", "")]
        public void IncompleteSentinelConfigurationIsRejected(string? service, string? endpoint)
        {
            var options = CreateOptions(true);
            options.RedisServiceName = service;
            options.RedisSentinelEndpoints = endpoint is null ? [] : [endpoint];
            Assert.False(new OidcAuthOptionsValidator().Validate(options).IsValid);
        }

        [Theory]
        [InlineData("127.0.0.1:0")]
        [InlineData("127.0.0.1:65536")]
        [InlineData("sentinel:invalid")]
        [InlineData("redis://sentinel:26379")]
        public void InvalidSentinelEndpointIsRejected(string endpoint)
        {
            var options = CreateOptions(false);
            options.RedisServiceName = "auth";
            options.RedisSentinelEndpoints = [endpoint];
            Assert.False(new OidcAuthOptionsValidator().Validate(options).IsValid);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DirectConnectionRequiresHost(bool tokensOnly)
        {
            var options = CreateOptions(tokensOnly);
            var validator = new OidcAuthOptionsValidator();
            Assert.False(validator.Validate(options).IsValid);
            options.RedisHost = "127.0.0.1";
            Assert.True(validator.Validate(options).IsValid);
        }

        [Fact]
        public void DisabledRedisDoesNotRequireConnectionSettings()
        {
            var options = CreateOptions(false);
            options.EnableRedisDataProtection = false;
            Assert.True(new OidcAuthOptionsValidator().Validate(options).IsValid);
        }

        private static OidcIdentityServerModuleOptions CreateOptions(bool tokensOnly) => new()
        {
            OidcClientId = "test",
            OidcClientSecret = "test",
            EnableRedisDataProtection = !tokensOnly,
            TokenStoreType = tokensOnly ? TokenStoreType.Redis : TokenStoreType.None
        };
    }
}
